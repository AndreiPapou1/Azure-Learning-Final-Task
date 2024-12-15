using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Azure.Messaging.ServiceBus;
using Azure.Messaging;
using System;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.FirstOrDefaultAsync(basketSpec);

        Guard.Against.Null(basket, nameof(basket));
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        await NotifyServiceBus(order);
        await NotifyDeliveryProcessor(order);
        
    }

    private async Task NotifyServiceBus(Order order)
    {
        using (var httpClient = new HttpClient())
        {
            var client = new ServiceBusClient(Environment.GetEnvironmentVariable("ServiceBusConnectionString"));
            ServiceBusSender sender = client.CreateSender("orderreservesqueue");

            var message = new ServiceBusMessage(JsonSerializer.Serialize(new
            {
                id = order.Id,
                ShippingAddress = order.ShipToAddress,
                Items = order.OrderItems,
                FinalPrice = order.Total(),
                BuyerId = order.BuyerId
            }));

            await sender.SendMessageAsync(message);
        }
    }

    private async Task NotifyDeliveryProcessor(Order order)
    {
        using (var httpClient = new HttpClient())
        {
            StringContent body = new(
                JsonSerializer.Serialize(new
                {
                    id = order.Id,
                    ShippingAddress = order.ShipToAddress,
                    Items = order.OrderItems,
                    FinalPrice = order.Total(),
                    BuyerId = order.BuyerId
                }),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync($"{Environment.GetEnvironmentVariable("DeliveryOrderProcessorUrl")}/api/ReserveDeliveryOrder", body);
        }
    }
}
