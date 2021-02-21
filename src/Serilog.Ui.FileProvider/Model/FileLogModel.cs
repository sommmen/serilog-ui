using Serilog.Ui.Core;

namespace Serilog.Ui.FileProvider.Model
{
    /// <summary>
    /// <see cref="LogModel"/> implementation for the <see cref="FileDataProvider"/>
    /// </summary>
    internal class FileLogModel : LogModel
    {
        /// <summary>
        /// The filepath of to the file this row was read from
        /// </summary>
        public virtual string FilePath { get; set; }
    }
}