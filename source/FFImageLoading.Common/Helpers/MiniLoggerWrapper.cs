using System;
using FFImageLoading.Helpers;
namespace FFImageLoading
{
    [Preserve(AllMembers = true)]
    internal class MiniLoggerWrapper : IMiniLogger
    {
        IMiniLogger _logger;
        bool _verboseLogging;

        public MiniLoggerWrapper(IMiniLogger logger, bool verboseLogging)
        {
            _logger = logger;
            _verboseLogging = verboseLogging;
        }

        public void Debug(string message)
        {
            if (_verboseLogging)
                _logger.Debug(message);
        }

        public void Error(string errorMessage)
        {
            _logger.Error(errorMessage);
        }

        public void Error(string errorMessage, Exception ex)
        {
            _logger.Error(errorMessage, ex);
        }
    }
}

