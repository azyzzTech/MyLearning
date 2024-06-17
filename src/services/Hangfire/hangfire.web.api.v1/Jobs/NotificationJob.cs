namespace hangfire.web.api.v1.Jobs
{
    public class NotificationJob
    {
        public void SendNotification()
        {
            Console.WriteLine("Notification sent after job completion.");
        }
    }
}
