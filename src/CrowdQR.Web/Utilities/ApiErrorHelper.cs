namespace CrowdQR.Web.Utilities;

/// <summary>
/// Utility class for handling API errors.
/// </summary>
public static class ApiErrorHelper
{
    /// <summary>
    /// Gets a user-friendly error message for API errors.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="defaultMessage">The default message to return.</param>
    /// <returns>A user-friendly error message.</returns>
    public static string GetUserFriendlyErrorMessage(Exception exception, string defaultMessage = "An unexpected error occurred. Please try again later.")
    {
        if (exception is HttpRequestException httpEx)
        {
            return httpEx.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound => "The requested resource was not found.",
                System.Net.HttpStatusCode.Unauthorized => "You are not authorized to perform this action.",
                System.Net.HttpStatusCode.Forbidden => "You don't have permission to access this resource.",
                System.Net.HttpStatusCode.BadRequest => "The request was invalid. Please check your inputs and try again.",
                System.Net.HttpStatusCode.InternalServerError => "The server encountered an error. Please try again later.",
                _ => defaultMessage
            };
        }

        if (exception is TaskCanceledException)
        {
            return "The request timed out. Please check your internet connection and try again.";
        }

        if (exception is OperationCanceledException)
        {
            return "The operation was canceled. Please try again.";
        }

        return defaultMessage;
    }
}