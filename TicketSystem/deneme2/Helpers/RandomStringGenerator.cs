namespace TicketSystem.Helpers
{
     public class RandomStringGenerator
     {
          private static readonly string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

          public static string Generate(int length)
          {
               var random = new Random();
               var result = new char[length];
               for (int i = 0; i < length; i++)
               {
                    result[i] = Characters[random.Next(Characters.Length)];
               }
               return new string(result);
          }
     }
}
