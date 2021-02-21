using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Ui.Core;
using Serilog.Ui.FileProvider.Helpers;
using Serilog.Ui.FileProvider.Model;

namespace Serilog.Ui.FileProvider
{
    public class FileDataProvider : IDataProvider
    {
        private readonly IFileDataProviderOptions _options;

        public FileDataProvider(IFileDataProviderOptions options)
        {
            _options = options;
        }

        private IEnumerable<FileInfo> GetFiles(IEnumerable<string> logDirectories)
        {
            // TODO Option to cache this result for daily rolled log files https://docs.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache?view=dotnet-plat-ext-5.0
            // TODO Find a way to handle file patterns better, it would be nice to be able to do: *.txt|*.log|*log*|*-log*
            // NOTE We assume the last write time is the last time something was logged. This can be messed up if the user modifies older log files.
            //      The only solution to that would be to go through all files, and memory/performance wise that would be difficult.
            //      Aside from that there is no guarantee that the user has a log format that includes the datetime for each row.
            //      Since an use case where older log files would be modified would not be usual - we simply make this assumption.
            // NOTE OrderByDescending destroys the 'laziness' of enumerateFiles. Is there a windows API that allows quick file enumeration with ordering on LastModified?
            // NOTE For optimization, this assumes a single directory with a bunch of files (I.e. a month's worth, or 30 files) - Subdirectories are not a likely use case.
            return logDirectories
                .SelectMany(logDirectory => new DirectoryInfo(logDirectory)
                    .EnumerateFiles(_options.SearchPattern, _options.SearchOptions)
                    .OrderByDescending(fileInfo => fileInfo.LastWriteTime)
                );
        }

        private int GetTotalRowsAmount()
        {
            // ASSUMPTION Older log files than the current log file are not modified and therefore have a constant log count.
            // ASSUMPTION Log row count per file does not exceed Int32.MaxValue

            // TODO Caching. We can cache the rowcount for all files except the current active log file (highest lastModified).

            var files = GetFiles(_options.LogDirectories);

            var currentLogFile = files.FirstOrDefault();

            // No logfiles (yet) return 0. TODO Discuss desired exception behaviour - Throw? log? swallow?
            if (currentLogFile == null)
                return 0;


            
            var count = 0;
            var countSyncLock = new object();

            _ = Parallel.ForEach(files, (fileInfo) =>
            {
                var newLinesCount = StreamHelpers.CountLines(File.OpenRead(fileInfo.FullName));
                lock (countSyncLock)
                {
                    count += Convert.ToInt32(newLinesCount); // TODO check if this works with closure (from the docs parallel.foreach should handle this)
                }
            });

            return count;
        }

        public async Task<(IEnumerable<LogModel>, int)> FetchDataAsync(
            int page,
            int count,
            string logLevel = null,
            string searchCriteria = null
        )
        {
            // NOTE count is the pageCount (# rows in a page)
            // TODO ask author if this can be renamed to pageCount
            //      Also the tuple returned would be better as a class/interface for clarity and extensibility. Discuss this with author.
            // TODO This assumes one log entry per line - the default for the serilog file sink is that exceptions go on multiple lines....
            //      How to deal with that?

            var totalRowsAmount = GetTotalRowsAmount();
            
            // TODO A good optimization would be to store the row count per file
            //      That way we can quickly figure out what file(s) we need to approach (and what line)
            //      This would also allow some palatalization instead of having to sequentially iterate all files...
            // TODO For very large log files (gigabytes) memory mapped file should be better (says google).
            //      Its more likely that files are rolled and won't reach those filesizes, discuss this.

            var linesToSkip = page * count;
            var linesToRead = count;

            var fileInfos = GetFiles(_options.LogDirectories);

            var result = new Queue<FileLogModel>();

            string line;

            foreach (var fileInfo in fileInfos)
            {
                // var fileStream = File.OpenRead(fileInfo.FullName);
                using (var fileStreamReader = new StreamReader(fileInfo.FullName))
                {
                    line = fileStreamReader.ReadLine();

                    if(string.IsNullOrWhiteSpace(line)) 
                        continue; // TODO test this. What happens if there is only one file and its empty?

                    // Skip lines till we hit the beginning of the page
                    while (linesToSkip > 0 && (line = fileStreamReader.ReadLine()) != null)
                    {
                        // Do nothing - we simply read lines and discard the value.
                    }

                    // TODO check for off-by-one error.

                    // Beginning of the page, start
                    do
                    {
                        // TODO LEFT OFF HERE Add parsing of line here.... This will be specific to the output format (so user).
                        //      Perhaps pass behaviour into the options?

                        linesToRead--;

                        result.Enqueue(new FileLogModel
                        {
                            RowNo = linesToSkip + linesToRead,
                        });
                        
                    } while(linesToRead > 0 && (line = fileStreamReader.ReadLine()) != null);

                    if(linesToRead <= 0)
                        break;
                }
            }

            return (result, totalRowsAmount); 
        }

    }
}