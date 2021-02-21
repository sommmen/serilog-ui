using System.Collections.Generic;
using System.IO;

namespace Serilog.Ui.FileProvider
{
    /// <summary>
    /// TODO document
    /// </summary>
    public interface IFileDataProviderOptions
    {
        IReadOnlyCollection<string> LogDirectories { get; }
        string SearchPattern { get; }
        SearchOption SearchOptions { get; set; }
    }

    /// <inheritdoc cref="IFileDataProviderOptions"/>
    class FileDataProviderOptions : IFileDataProviderOptions
    {
        /// <inheritdoc />
        public IReadOnlyCollection<string> LogDirectories { get; set; }

        /// <inheritdoc />
        public string SearchPattern { get; set; } = "*.txt";

        /// <inheritdoc />
        public SearchOption SearchOptions { get; set; } = SearchOption.AllDirectories;
    }
}
