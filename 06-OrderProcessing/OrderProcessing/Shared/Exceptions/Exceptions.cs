namespace OrderProcessing.Shared.Exceptions;
public class CustomerNotFoundException(string message) : Exception(message);
public class InvalidCouponException(string message) : Exception(message);
public class ShippingProviderUnavailableException(string message) : Exception(message);
public class DatabaseWriteException(string message) : Exception(message);
public class SmtpUnavailableException(string message) : Exception(message);
public class DataFileCorruptedException(string message, Exception? inner = null) : Exception(message, inner);
