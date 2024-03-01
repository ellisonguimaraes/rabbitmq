using MassTransit;

namespace PublishInMemory;

public class Program
{
    public static void Main(string[] args)
    {
        Process();
        Console.ReadLine();
    }

    static async void Process()
    {
        var bus = Bus.Factory.CreateUsingInMemory(sbc => 
        {
            sbc.ReceiveEndpoint("order", ep => 
            {   
                ep.Handler<Message>(context => 
                {
                    return Console.Out.WriteLineAsync($"Received: {context.Message.Text}");
                });
            });
        });

        await bus.StartAsync();

        try
        {   
            var index = 0;

            while(true)
            {
                object p = bus.Publish(new Message
                {
                    OrderId = index,
                    Text = $"{DateTime.UtcNow} => message index order: {index++}"
                });

                await Task.Run(() => Thread.Sleep(1000));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            await bus.StopAsync();
        }
    }
}
