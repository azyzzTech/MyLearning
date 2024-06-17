namespace hangfire.web.api.v1.Jobs
{
    public class CancellableJob
    {
        public void Execute(CancellationToken cancellationToken)
        {
            for (int i = 0; i < 10; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Job was cancelled.");
                    return;
                }

                Console.WriteLine($"Job iteration {i}");
                Thread.Sleep(1000);
            }
        }
    }
}
