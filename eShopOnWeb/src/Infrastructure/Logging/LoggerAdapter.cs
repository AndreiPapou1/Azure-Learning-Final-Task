﻿using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace Microsoft.eShopWeb.Infrastructure.Logging;

public class LoggerAdapter<T> : IAppLogger<T>
{
    private readonly ILogger<T> _logger;
    public LoggerAdapter(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<T>();
    }

    public void LogWarning(string message, params object[] args)
    {
        System.Diagnostics.Trace.TraceWarning(message, args);
        _logger.LogWarning(message, args);
    }

    public void LogInformation(string message, params object[] args)
    {
        System.Diagnostics.Trace.TraceInformation(message, args);
        _logger.LogInformation(message, args);
    }
}
