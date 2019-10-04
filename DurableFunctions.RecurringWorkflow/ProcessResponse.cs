namespace DurableFunctions.Recurring
{
    public class ProcessResponse
    {
        public string Status { get; }
        public string Message { get; }
        public int NextCheckInterval { get;  }

        public ProcessResponse(string status, string message = null, int? nextCheckInterval = null) =>
            (Status, Message, NextCheckInterval) = (status, message, nextCheckInterval ?? 10);
    }
}