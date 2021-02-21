using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Ui.Core;

namespace Serilog.Ui.FileProvider.Extensions
{
    public static class SerilogUiOptionBuilderExtensions
    {
        public static void UseFile(
            this SerilogUiOptionsBuilder optionsBuilder,
            IEnumerable<string> logDirectories
        )
        {
            IFileDataProviderOptions options = new FileDataProviderOptions()
            {
                LogDirectories = logDirectories.ToList()
            };

            if (!options.LogDirectories.Any())
                throw new InvalidOperationException("Specify at least one log directory");

            ((ISerilogUiOptionsBuilder)optionsBuilder).Services.AddSingleton(options);
            ((ISerilogUiOptionsBuilder)optionsBuilder).Services.AddScoped<IDataProvider, SqlServerDataProvider>();
        }
    }
}