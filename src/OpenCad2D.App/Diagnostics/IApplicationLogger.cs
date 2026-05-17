using System;

namespace OpenCad2D.App.Diagnostics;

public interface IApplicationLogger
{
    void Info(string category, string message);

    void Warning(string category, string message, Exception? exception = null);

    void Error(string category, string message, Exception exception);
}
