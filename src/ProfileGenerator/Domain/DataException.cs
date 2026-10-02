namespace ProfileGenerator.Domain;

public sealed class DataException(string message, Exception? innerException = null)
    : Exception(message, innerException);
