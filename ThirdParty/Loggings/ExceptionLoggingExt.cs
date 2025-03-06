using System;

namespace ThirdParty.Loggings
{
    internal static class ExceptionLoggingExt
    {
        public static string Format(
                this Exception exception
            )
        {
            var innerException = exception.InnerException;
            if (innerException == null)
            {
                return
                    string.Format(
                        "{0}: {1}\n{2}",
                        exception.GetType().FullName,
                        exception.Message,
                        exception.StackTrace
                    );
            }
            else
            {
                return 
                    string.Format(
                        "{0}: {1}\n{2}\n\nWith inner exception {3}: {4}\n{5}",
                        exception.GetType().FullName,
                        exception.Message,
                        exception.StackTrace,
                        innerException.GetType().FullName,
                        innerException.Message,
                        innerException.StackTrace
                    );
            }
        }
    }
}
