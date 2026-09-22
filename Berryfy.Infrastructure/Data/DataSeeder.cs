namespace Berryfy.Infrastructure.Data
{
    public class DataSeeder
    {
        public Task SeedDataAsync()
        {
            Console.WriteLine("DataSeeder skipped: EF Identity seeding was removed in favor of raw SQL repositories.");
            return Task.CompletedTask;
        }
    }
}
