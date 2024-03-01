# RabbitMQ

## 1. Producer e Consumer Básicos

Nesse tópico, iremos criar um *producer* e um *consumer* básicos em **.NET**, começando com o *producer* (usando um projeto *console*).

> Para usar o **RabbitMQ** no **.NET** é necessário instalar o seguinte pacote: `dotnet add package RabbitMQ.Client`.

Tanto no *producer* quanto no *consumer*, é necessário fazer as configurações iniciais básicas. Esse código vai se repetir em ambos:

- Criar o *factory* com `ConnectionFactory`. Nela é necessário informar as informações de conexão com o **RabbitMQ**, como: 

  - `UserName` que é o *user name* do acesso ao **RabbitMQ**. Se for a senha padrão *guest*, não é necessário informar;
  - `Password` que é a senha de acesso ao **RabbitMQ**. Essa também se *guest* não é necessário informar;
  - `HostName` que é o *host* que o **RabbitMQ** está;
  - `Port` sendo a porta de comunicação do **RabbitMQ**.

  A partir da *factory* que criaremos uma *connection*.

- Criar uma *connection*, usando o método da *factory* `factory.CreateConnection()`; 

- Criar um *channel*, usando a *connection* `connection.CreateModel()`;

- Declarar a fila que esse *consumer* irá trabalhar, nesse caso, somente a fila `order`, usando o *channel* através do `channel.QueueDeclare`.

A partir desse ponto, já se torna características específicas do *producer*. Veremos no código abaixo:

```csharp
var counter = 0;

var factory = new ConnectionFactory
{
    UserName = "ellison",
    Password = "Abcd@1234",
    HostName = "localhost", 
    Port = 5672
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare("order", false, false, false, null);

while (true)
{   
    var message = $"Message number {counter}.";
    var body = Encoding.UTF8.GetBytes(message);
    channel.BasicPublish(string.Empty, "order", null, body);

    Console.WriteLine($"Mensagem enviada: {message}");
    Console.WriteLine("Press [ENTER] to send message...");
    Console.ReadLine();

    counter++;
}
```

Observe que no *producer*, basta usarmos o `channel` para publicar uma mensagem usando o método `channel.BasicPublish`.

> Mais pra frente iremos entender todos os parâmetros do método `BasicPublish`, por enquanto, veremos só o uso básico dele, usando a *exchange default* (que não precisamos informar o nome).

Finalmente, o *consumer*: dessa vez, é um projeto do tipo `worker` *services*, sendo o nosso *worker*:

```csharp
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            UserName = "ellison",
            Password = "Abcd@1234",
            HostName = "localhost",
            Port = 5672
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare("order", false, false, false, null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += Consumer_Received;

        channel.BasicConsume("order", true, consumer);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                $"Worker active: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            await Task.Delay(10000, stoppingToken);
        }
    }

    private void Consumer_Received(object? sender,
                                   BasicDeliverEventArgs e)
    {
        var message = Encoding.UTF8.GetString(e.Body.Span);
        Console.WriteLine($"Received message: {message}");
    }
}

```

Observe que no nosso *worker*, também tem as configurações básicas de *factory*, *connection*, *channel* e a declaração da fila que o *channel* vai usar. A parte específica do *consumer* fica após a declaração da fila:

- Criando um *consumer* com `EventingBasicConsumer`, informando o *channel*;
- Atribuindo um método de *callback* ao `consumer.Received`. Esse método será executado toda vez que receber uma mensagem;
- Declaramos um `channel.BasicConsume` informando qual o *consumer* e para qual fila esse *consumer* vai ser executado.

## 2. Mensageria como Eventos

Podemos abstrair messageria através de eventos, dessa forma, podemos:

- Publicar um evento, e automaticamente o ***bus*** entende para qual fila enviar a mensagem;
- Criar um consumidor para o evento, ou seja, registrar um *consumer* para o tipo determinado do evento.

Esse tipo de abstração contém alguns componentes como:

- `Event` que é a descrição do que será executado (algo parecido com o *command*);
- `EventHandler` que é a ação executada quando a fila reconhecer o evento; 
- `Bus` que é a classe que terá as operações de publicação e leitura da fila.

Abaixo veremos um exemplo desse tipo de abstração em .NET.

### 2.1. Event e EventHandler

O `Event` e o `EventHandler` é a base dessa abstração. O `Event` é a identificação (e os dados básicos, assim como um *command*) de qual evento será executado. Nesse caso, automaticamente o nome dela será associada a uma fila no *broker*. Já o `EventHandler` é a ação executada ao receber determinado `Event` (assim como um *command handler*).

Abaixo, criaremos uma classe abstrata `Event` e será usada para a criação de eventos que serão trafegados pelo *bus*. Trataremos filas como um `Event`, ou seja, o *payload* trafegado será um objeto do tipo `Event` e o nome da fila será o mesmo nome do `Event`. Veja:

```csharp
public abstract class Event
{
    public DateTime Timestamp { get; protected set; } 
    	= DateTime.UtcNow;
}
```

Observe que é uma classe abstrata, ou seja, criaremos eventos que herdará de `Event`. 

Também temos o `IEventHandler<T>` que é a definição do contrato para o *handler* do evento que será executado após a aplicação receber a mensagem (no *subscribe*, que veremos posteriormente).

```csharp
public interface IEventHandler<in TEvent> : IEventHandler 
    where TEvent : Event
{
    Task Handle(TEvent @event);
}

public interface IEventHandler { }
```

Dessa forma, se temos um evento `OrderEvent`, esse derivado da classe abstrata `Event`, terá um `OrderEventHandler` derivado da interfa `IEventHandler<OrderEvent>`. 

### 2.2. Bus

O *Bus* como comentamos anteriormente, é a abstração responsável pela lógica de publicação e de inscrição do evento no *broker* (de escutar a fila).

Abaixo veremos o `IBus`, o contrato que definirá os métodos básicos do *Bus*: 

- O `Publish<T>(T @event)` que realizará a publicação da mensagem em uma fila, que nesse caso, a fila terá o mesmo nome do evento;
- O `Subscribe<T, THandler>` que prepara a aplicação para escutar a fila (com o mesmo nome do evento `T`).

```csharp
public interface IBus
{
    void Publish<T>(T @event) where T : Event;

    void Subscribe<T, THandler>()
        where T : Event
        where THandler : IEventHandler<T>;
}
```

Abaixo temos uma implementação do `RabbitMQBus`, baseado na interface acima `IBus`. Nessa primeira parte o `Publish<T>`, considerando que `T` é do tipo `Event`. Ou seja, só poderei publicar um `Event` (objeto que irá ser trafegado pelo *bus*).

```csharp
namespace Bus.Infra.Bus.RabbitMQ;

public class RabbitMQBus(
    RabbitMQSettings settings,
    IServiceScopeFactory serviceScopeFactory) : IBus
{   
    #region Dependency Injection
    private readonly RabbitMQSettings _settings = settings;
    private readonly IServiceScopeFactory _serviceScopeFactory 
        = serviceScopeFactory;
    #endregion

    private Dictionary<string, List<Type>> _handlers = [];
    private List<Type> _eventTypes = [];

    public void Publish<T>(T @event) where T : Event
    {
        var factory = CreateConnectionFactory();

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var eventName = @event.GetType().Name;

        channel.QueueDeclare(eventName, false, false, false, null);

        var message = JsonConvert.SerializeObject(@event);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(string.Empty, eventName, null, body);
    }

    public void Subscribe<T, THandler>()
        where T : Event
        where THandler : IEventHandler<T>
    {
        var eventType = typeof(T);
        var handlerType = typeof(THandler);

        if (!_eventTypes.Contains(eventType)) 
            _eventTypes.Add(eventType);

        if (!_handlers.ContainsKey(eventType.Name)) 
            _handlers.Add(eventType.Name, []);

        if (_handlers[eventType.Name].Any(s =>
      		s.GetType().Equals(handlerType)))
            throw new ArgumentException($"Handler exception {handlerType.Name} was previously registered by '{eventType.Name}'", nameof(handlerType));

        _handlers[eventType.Name].Add(handlerType);

        StartBasicConsume<T>();
    }

    private void StartBasicConsume<T>() where T : Event
    {
        var factory = CreateConnectionFactory();

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        var eventName = typeof(T).Name;

        channel.QueueDeclare(eventName, false, false, false, null);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.Received += async (sender, @event) => {
            var eventName = @event.RoutingKey;
            var message = Encoding.UTF8.GetString(@event.Body.Span);

            try
            {
                await ProcessEvent(eventName, message)
                    .ConfigureAwait(false);
                channel.BasicAck(@event.DeliveryTag, false);
            }
            catch (Exception)
            {
                channel.BasicNack(@event.DeliveryTag, false, true);
            }
        };

        channel.BasicConsume(eventName, false, consumer);
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (_handlers.TryGetValue(eventName, out List<Type>? subscriptions))
        {
            using var scope = _serviceScopeFactory.CreateScope();

            foreach (var sub in subscriptions)
            {
                var handler = scope.ServiceProvider.GetService(sub);
                
                if (handler is null) continue;

                var eventType = _eventTypes.Single(t => 
              		t.Name.Equals(eventName));
                var @event = JsonConvert
                    .DeserializeObject(message, eventType);
                var concreteType = typeof(IEventHandler<>)
                    .MakeGenericType(eventType);
                
                await (Task)concreteType.GetMethod("Handle")!
                    .Invoke(handler, [@event!])!;
            }
        }
    }

    private ConnectionFactory CreateConnectionFactory()
        => new () {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            DispatchConsumersAsync = true
        };
}
```

Nessa segunda parte o `Subscribe<T, THandler>`. Considerando que `T` é do tipo `Event` e `THandler` o *handler* que será executado quando o `Event` (fila) for escutada.

> ==Observação!==
>
> Para essa implementação, estamos considerando o nome do `Event` como o mesmo nome da fila.

Observe que no primeiro momento é feito validações para evitar que a aplicação registre duas vezes o mesmo evento para escutar. Ao final, chama o método `StartBasicConsume<T>()` que cria a conexão e o canal, declara a fila que irá ser escutada, atribui um método de *callback* `Consumer_Received` e inicia um `BasicConsume` informando o nome da fila (evento) e o *consumer*.

O método de *callback* `Consumer_Received` será o método que é acionado assim que um item da fila for ouvido pela aplicação. Nele deve conter toda a tratativa ao ler esse evento. Observe que ele chama o método `ProcessEvent`, método este que cria uma instância do *handler* (do `EventHandler`) e executa o método `Handle()` informando o evento.

> ==Observação!==
>
> No `Subscriber()` não é possível utilizar *using* para ao declarar *connection* ou *channel*, pois ela encerra a sessão e para de "*escutar*" a fila. Só é possível utilizar quando o *bus* é implementado com *worker*.

### 2.3. Usando o Publish

Para exemplificar o consumo do *bus*, criamos o evento `ReportOccurenceEvent` com suas propriedades que serão trafegadas na fila. É importante observar que um evento derivará da classe abstrata `Event` e a fila conterá o mesmo nome `ReportOccurenceEvent`.

```csharp
namespace Ocurrences.Domain.Events;

public class ReportOccurenceEvent(
    string licensePlate, 
	string description, 
  	OccurrenceType type) : Event
{
    public string LicensePlate { get; set; } = licensePlate;

    public string Description { get; set; } = description;

    public OccurrenceType Type { get; set; } = type;
}
```

Para publicar o evento, basta utilizarmos a injeção do `IBus` (que faremos no próximo tópico), criar o evento do tipo `ReportOccurenceEvent` e utilizar o método `Publish(event)` do *bus* passando o evento. Veja abaixo:

```csharp
namespace Report.Domain.CommandHandlers;

public class ReportOcurrenceCommandHandler(IBus bus) : IRequestHandler<ReportOccurenceCommand>
{
    private readonly IBus _bus = bus;

    public Task Handle(ReportOccurenceCommand request, CancellationToken cancellationToken)
    {
        var @event = new ReportOccurenceEvent(
            request.LicensePlate, 
            request.Description, 
            request.Type);
            
        _bus.Publish(@event);
        return Task.FromResult(true);
    }
}
```

### 2.4. Usando o Consumer

No *consumer*, além de termos o evento `ReportOccurenceEvent`, precisamos ter o seu *handler* `ReportOccurenceEventHandler` que será a ação executada quando o *consumer* receber um item da fila para ser processada.

Abaixo, podemos observar que a ação será salvar os dados no banco:

```csharp
namespace Ocurrences.Domain.EventHandlers;

public class ReportOccurenceEventHandler(IOcurrenceRepository repository) : IEventHandler<ReportOccurenceEvent>
{
    private readonly IOcurrenceRepository _repository = repository;
    
    public async Task Handle(ReportOccurenceEvent @event)
    {
        Console.WriteLine("Reported occurence vehicle: " + @event.LicensePlate);

        var ocurrence = new Ocurrence {
            Description = @event.Description,
            LicensePlate = @event.LicensePlate,
            Type = @event.Type
        };

        await _repository.CreateAsync(ocurrence);
    }
}
```

Agora, precisamos configurar na aplicação que ela escutará essa fila/evento. Para isso, precisamos injetar no `program.cs` o *event handler*:

```csharp
builder.Services.AddTransient<IEventHandler<ReportOccurenceEvent>, ReportOccurenceEventHandler>();

//Subscriptions
builder.Services.AddTransient<ReportOccurenceEventHandler>();
```

Finalmente, precisamos indicar o registro no `RabbitMQ` que a aplicação (o *consumer*) escutará o evento. Obtendo uma instância de *bus* e dando um `Subscribe<>` para o evento `ReportOccurenceEvent`:

```csharp
var bus = app.Services.GetRequiredService<IBus>();
bus.Subscribe<ReportOccurenceEvent, ReportOccurenceEventHandler>();	
```

### 2.5. Injetando as Dependêncais

> ==Observação!==
>
> Para utilizar o **RabbitMQ** com **.NET** é necessário instalar o pacote `RabbitMQ.Client`. Para isso use o comando: `dotnet add package RabbitMQ.Client`. 

A classe `RabbitMQSettings` contém as informações básicas de conexão ao servidor do **RabbitMQ**. Este objeto será criado a partir dos dados que estarão no `appsettings.json` e que serão repassadas na injeção de dependência para injetar o *bus* de **RabbitMQ**.

```csharp
public class RabbitMQSettings
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = default;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

Abaixo é um *extension method* para `IServiceCollection`. Observe que essa extensão recebe um objeto do tipo `RabbitMQSettings`.

Nessa extensão é realizado a injeção do `IBus` com sua implementação `RabbitMQus`. Dentro, obtemos um serviço do tipo `IServiceScopeFactory` que usaremos dentro do *bus* para criar instâncias dos *event handlers* a serem executados (quando uma item da fila for escutado).

```csharp
public static class BusDependencyInjectionExtensions
{
    public static IServiceCollection RegisterRabbitMQBus(
        this IServiceCollection services, 
        RabbitMQSettings settings)
    {
        services.AddSingleton<IBus, RabbitMQBus>(factory => {
            var scopeFactory = factory
                .GetRequiredService<IServiceScopeFactory>();
            return new RabbitMQBus(settings, scopeFactory);
        });

        return services;
    }
}
```

Também, em ambos os projetos do tópico anterior (*producer* e *consumer*) é necessáro a injeção do *bus* do **RabbitMQ** usando a *extensions* que criamos acima. Em cada um dos projetos:

```csharp
var settings = builder.Configuration.GetSection("RabbitMQSettings")
    .Get<RabbitMQSettings>();
builder.Services.RegisterRabbitMQBus(settings!);
```


---

## 3. Container com RabbitMQ

Para criar um *container* com RabbitMQ, basta executar o comando abaixo:

```bash
docker run -d --hostname rabbit-server --name rabbit-broker -p 5672:5672 -p 15672:15672 rabbitmq:3.9.12-management
```

Onde:

- A porta **5672** mapeada serve para comunicação da aplicação com o **RabbitMQ**;
- A porta **15672** mapeada para acesso ao ambiente gráfico de gerenciamento.

Para acessar o *container* internamente:

```bash
docker exec -it rabbit-broker bash
```

Entrando no *container* podemos executar alguns comandos. Abaixo será detalhado cada um deles.

### 3.1. Listar e ativar plugins

O **RabbitMQ** contém vários *plugins* que podemos utilizar, porém, poucos deles vem habilitado por padrão. 

Podemos listar com o comando:

``` bash
rabbitmq-plugins list
```

Abaixo uma ilustração do resultado do comando acima. Observe que poucos estão habilitados, e um deles é o `rabbitmq_management` que é a interface gráfica que utilizamos.

![image-20240224021618751](RabbitMQ.assets/image-20240224021618751.png)

É possível ativar ou desativar usando o comando:

```bash
rabbitmq-plugins <enable/disable> <plugin_name>
```

> É possível verificar quais comandos podemos fazer com o `rabbitmq-plugins`, usando o comando `rabbitmq-plugins help`.

### 3.2. Manipulando usuários

Por padrão, o **RabbitMQ** quando criado vem com o usuário *guest* (de senha *guest*) para comunicação e acesso ao *manager*. Dentro do *container* é possível excluí-lo e criar novos usuários. Veremos a seguir alguns comandos para isso.

Podemos criar um novo usuário:

```bash
rabbitmqctl add_user <name> <password>
```

Observe que assim que criar o usuário, é exibida uma mensagem informando sobre a permissão e um comando para entender como configurar as permissões dele. Inicialmente, precisamos indicar como administrador:

```bash
rabbitmqctl set_user_tags <name> administrator
```

E setar a permissão total dele com o comando:

```bash
rabbitmqctl set_permissions -p / <name> "." "." ".*"
```

> Para mais comandos com permissões, use o comando `rabbitmqctl help set_permissions`.

Um comando básico e também necessário é remover um usuário. Normalmente usado para excluir o usuário *guest*. Basta:

```bash
rabbitmqctl delete_user <name>
```

---

## 4. CloudAMQP

O **CloudAMQP** é um "*RabbitMQ as service*", ou seja, **RabbitMQ** como serviço. É possível acessar através da *url* https://www.cloudamqp.com/. É uma plataforma que automatiza toda a configuração, operação e escalonamento do **RabbitMQ**. 

![image-20240224025935612](RabbitMQ.assets/image-20240224025935612.png)

> É possível utilizar a plataforma gratuitamente para testes.

---

## 5. ACK/NACK/AUTOACK

Existe uma configuração no **RabbitMQ** denominada **AutoAck**. Essa configuração é feita assim que criamos um *consumer*. Com essa configuração ativa, o **RabbitMQ** considera somente o consumo da mensagem, desconsiderando se a aplicação conseguiu processar ou não o item da fila, ou seja, caso ocorra um erro no processamento da mensagem na aplicação, o item não volta a fila e consequentemente é perdido (caso não haja uma tratativa na aplicação).

Abaixo um *basic consumer* para exemplificar e mostrar onde fica a configuração do **AutoAck**:

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "ellison",
    Password = "Abcd@1234"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare("order", false, false, false, null);

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, ea) => {
    var body = ea.Body.Span;
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($"Received message: {message}");
};

channel.BasicConsume(
    queue: "order", 
    autoAck: true, 
    consumer: consumer);

Console.WriteLine("Press [ENTER] to exit...");
Console.ReadLine();
```

> ==Observação!==
>
> Caso não haja a tratativa do *ack*/*nack*, não deixe o **AutoAck** desativado (igual a `false`), pois os itens sempre retornarão a fila, mesmo que seja processado com sucesso.

### 5.1. Ack e Nack

Desativando o **AutoAck**, conseguimos configurar manualmente se a mensagem foi processada com sucesso ou não para o **RabbitMQ**. Dessa forma, se o *consumer* ler um item na fila e após um tempo nã o receber a confirmação (o *ack*) ou receber a confirmação de retornar a fila (o *nack*), automaticamente o item retorna a fila.

Dessa forma, utilizamos *ack* para confirmar que a mensagem foi processada com sucesso e *nack* para indicar ao **RabbitMQ** para o item retornar a fila. Abaixo um exemplo:

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "ellison",
    Password = "Abcd@1234"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare("order", false, false, false, null);

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, ea) => {
    try
    {
        var body = ea.Body.Span;
        var message = Encoding.UTF8.GetString(body);
        var number = int.Parse(message);

        Console.WriteLine($"Received message: {number}");

        channel.BasicAck(
            deliveryTag: ea.DeliveryTag, 
            multiple: false);
    }
    catch (Exception)
    {
        channel.BasicNack(
            deliveryTag: ea.DeliveryTag, 
            multiple: false, 
            requeue: true);
    }
};

channel.BasicConsume(
    queue: "order", 
    autoAck: false, 
    consumer: consumer);

Console.WriteLine("Press [ENTER] to exit...");
Console.ReadLine();
```

Dentro do `try` contém um método `.BasicAck()` informando a `DeliveryTag` (identificador da mensagem/item da fila) que confirma o processamento da mensagem com sucesso. Caso contrário, se ocorrer um `Exception` será retornado um `.BasicNack()` (informando também a `DeliveryTag`) e automaticamente o item retorna para a fila.

> ==Observação!==
>
> Com o código acima, caso a mensagem não seja processada por um error na mensagem, sempre será feita o *redelivery* da mensagem e o *consumer* executará em *looping*. Uma das formas de evitar isso é:
>
> - Usando o *redelivered*/*reject* no tópico abaixo;
> - Fazer uma tratativa de mensagens que deu erro no seu consumo. É possível observar uma das estratégias no tópico de *Message Looping*.

### 5.2. Redelivered e Reject

Caso necessário, podemos verificar se o item que está na fila já passou por um processamento, ou seja, se ele é um item de *redelivery*. É uma propriedade presente na mensagem, o `ea.Redelivered`.

Além disso, podemos rejeitar uma mensagem para ela não retornar a fila usando `channel.BasicReject()`, informando a `DeliveryTag`. Abaixo um exemplo:

```csharp
var factory = new ConnectionFactory
{
    HostName = "localhost",
    Port = 5672,
    UserName = "ellison",
    Password = "Abcd@1234"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.QueueDeclare("order", false, false, false, null);

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, ea) =>
{
    try
    {
        var body = ea.Body.Span;
        var message = Encoding.UTF8.GetString(body);
        var number = int.Parse(message);

        Console.WriteLine($"Received message: {number}");

        channel.BasicAck(
            deliveryTag: ea.DeliveryTag,
            multiple: false);
    }
    catch (Exception)
    {
        if (ea.Redelivered)
        {
            channel.BasicReject(
                deliveryTag: ea.DeliveryTag,
                requeue: false);
        }
        else
        {
            channel.BasicNack(
                deliveryTag: ea.DeliveryTag,
                multiple: false,
                requeue: true);
        }
    }
};

channel.BasicConsume(
    queue: "order",
    autoAck: false,
    consumer: consumer);

Console.WriteLine("Press [ENTER] to exit...");
Console.ReadLine();
```

> ==Observação!==
>
> O segundo parâmetro de `BasicReject` e o terceiro do `BasicNack` é referente a *requeue*. Quando `true` serão re-processadas na fila.

---

## 6. Multiplos Connections / Channels / Consumers

É possível, na sua aplicação, existir multiplas *connections*, *channels* ou *consumers*. Porém, temos que nos atentar a: 

- No *producer*: 
  - Um *producer* precisa ter seu canal isolado, ou seja, **um *channel* para um *producer***. Isso é representado na imagem abaixo com o **Produtor B** e o **Produtor C**;
  - Vários produtores (cada um com seu respectivo *channel*) pode compartilhar sim uma única conexão.
- No *consumer*:
  - Diferentemente do *producer*, um *consumer* pode compartilhar o mesmo *channel* sem problema algum;
  - Também, é possível compartilhar a mesma *connection*.

![image-20240225011100505](RabbitMQ.assets/image-20240225011100505.png)

> ==Observação!==
>
> Abaixo tem uma arquitetura de multiplicidade mais comum, onde cada um **Produtor** contém seu próprio *channel*, mas compartilha a *connection*. Já do lado do **Consumidor**, cada um com seu *channel* e sua *connection* separadas.
>
> ![image-20240225023831965](RabbitMQ.assets/image-20240225023831965.png)

### 6.1. Consumer

Abaixo temos a implementação do *consumer* considerando múltiplos *channels* e múltiplos *constumers*. No laço é observado que criamos 2 *channels* e 2 *consumers* para cada um deles, formando o total de 2 *channels* e *4 consumers*. Nesse caso, criamos uma única *connection*:

```csharp
var queueName = "order";

// Define um factory connection: informações para conexão com o servidor RabbitMQ
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "ellison",
    Password = "Abcd@1234",
    Port = 5672
};

// Cria a conexão
using var connection = factory.CreateConnection();

for(int i = 0; i < 2; i++)
{
    var channel = CreateChannel(connection, queueName);

    // Cria consumers
    for(int j = 0; j < 2; j++)
        CreateConsumer(channel, queueName, i, j);
}

Console.WriteLine("Press [ENTER] to exit consumer...");
Console.ReadLine();

static IModel CreateChannel(IConnection connection, string queueName)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define queue
    channel.QueueDeclare(queueName, false, false, false, null);

    return channel;
}

static void CreateConsumer(IModel channel, string queueName, int channelId, int consumerId)
{
    // Cria um consumer básico
    var consumer = new EventingBasicConsumer(channel);

    // Define o método que será executado quando receber um item
    consumer.Received += (model, ea) =>
    {
        try
        {
            var body = ea.Body.Span;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"[x] Received message (channel {channelId}, consumer {consumerId}): {message}");

            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception)
        {
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    };

    // Inicia o consumer
    channel.BasicConsume(queueName, false, consumer);
}
```

### 6.2. Producer

Aqui temos um exemplo de implementação (em .NET) de um *producer*. Esse código publica 10 mensagens toda vez que pressionado enter no console.

Poremos observar que assim como a regra citada acima, não estamos compartilhando um *channel* para vários produtores, **somente para um** que publica 10 mensagens seguidas (não em paralelo).

```csharp
var queueName = "order";
var counter = 0;

// Define um factory connection: informações para conexão com o servidor RabbitMQ
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "ellison",
    Password = "Abcd@1234",
    Port = 5672
};

using var connection = factory.CreateConnection();

var channel = CreateChannel(connection, queueName);

while(true)
{
    for (int i = 0; i < 10; i++)
        PublishMessage(channel, queueName, $"{counter++}");
    
    Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
    Console.ReadLine();
}

static void PublishMessage(IModel channel, string queueName, string message)
{
    // Define queue
    channel.QueueDeclare(queueName, false, false, false, null);

    var body = Encoding.UTF8.GetBytes(message);

    // Publish in queue
    channel.BasicPublish(string.Empty, queueName, null, body);
}

static IModel CreateChannel(IConnection connection, string queueName)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define queue
    channel.QueueDeclare(queueName, false, false, false, null);

    return channel;
}
```

> ==Observação!==
>
> Quando publicamos aquele canal vira um *buffer* entre a aplicação e o **RabbitMQ**. Por este motivo, não é recomendado ter dois *producers* para um mesmo *channel*, além de ocorrer erros durante o processo.

---

## 7. Prefetch Count

### 7.1. Entendendo o problema

Considerando o exemplo anterior, se publicarmos 10 mensagens na fila e após isso, executar o *consumer*, é possível observar o comportamento abaixo:

<img src="RabbitMQ.assets/image-20240225021827790.png" alt="image-20240225021827790" style="zoom: 67%;" />

Observe que apesar de ter 2 *channels* e 4 *consumers*, todos são executados no primeiro *consumer* do primeiro *channel*. Esse exemplo, demonstra somente com 10 mensagens, mas pode ocorrer com milhares até que se inicie o algoritmo de balanceamento de carga  **Round Robin**.

Com isso, o primeiro *consumer* será sobrecarregado e poderá ocorrer error, ou até mesmo não suportar a quantidade de leituras.

Diante disso, temos um problema iminente ao qual precisamos resolver. Para isso, precisamos entender o que é **Prefetch Count**.

### 7.2. O que é o Prefetch Count? 

O **Prefetch Count** é um limitador de mensagens não confirmadas a serem enviadas a consumidores que ficará em um *buffer* em memória, ou seja, é possível determinar através dela quantas mensagens por vez serão entregues ao consumidor (ou a cada consumidor).

Para resolvermos o problema anterior, precisaremos configurar o **Prefetch Count**. 

> ==Observação!==
>
> Além de configurar o **Prefetch Count**, precisamos também desligar o `AutoAck`, já que o **Prefetch Count** é "*limitador de mensagens não confirmadas a serem enviadas*. Dessa forma, não é possível usar com o `AutoAck` ativado.

### 7.3. Implementando um Consumer com Prefetch Count

Para adequar o *consumer* anterior a implementação/configuração do **Prefetch Count**, desligamos o `AutoAck` e passamos a tratar manualmente a confirmação de mensagem.

Finalmente, para configurar o **Prefetch Count**, após criar o *channel* utilizamos o comando:

```csharp
channel.BasicQos(0, 2, false);
```

<img src="RabbitMQ.assets/image-20240225021932061.png" alt="image-20240225021932061" style="zoom:67%;" />

O qual contém os parâmetros:

- `prefetchSize`: ainda não implementado no **RabbitMQ**;
- ` prefetchCount`: que é onde devemos limitar as mensagens enviadas a cada *consumer*;
- `global`: que quando `true`, ela limita a quantidade por *channel*, e quando `false` é limitada por *consumer*. Normalmente é calculada por *consumer*.

Abaixo implementamos essa configuração:

```csharp
var queueName = "order";

// Define um factory connection: informações para conexão com o servidor RabbitMQ
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "ellison",
    Password = "Abcd@1234",
    Port = 5672
};

// Cria a conexão
using var connection = factory.CreateConnection();

for(int i = 0; i < 2; i++)
{
    var channel = CreateChannel(connection, queueName);

    // Cria consumers
    for(int j = 0; j < 2; j++)
        CreateConsumer(channel, queueName, i, j);
}

Console.WriteLine("Press [ENTER] to exit consumer...");
Console.ReadLine();

static IModel CreateChannel(IConnection connection, string queueName)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define prefetch
    channel.BasicQos(0, 2, false);

    // Define queue
    channel.QueueDeclare(queueName, false, false, false, null);

    return channel;
}

static void CreateConsumer(IModel channel, string queueName, int channelId, int consumerId)
{
    // Cria um consumer básico
    var consumer = new EventingBasicConsumer(channel);

    // Define o método que será executado quando receber um item
    consumer.Received += (model, ea) =>
    {
        try
        {
            var body = ea.Body.Span;
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($"[x] Received message (channel {channelId}, consumer {consumerId}): {message}");

            channel.BasicAck(ea.DeliveryTag, false);
        }
        catch (Exception)
        {
            channel.BasicNack(ea.DeliveryTag, false, true);
        }
    };

    // Inicia o consumer
    channel.BasicConsume(queueName, false, consumer);
}
```

Executando o código já com o **Prefetch Count** o resultado:

![image-20240225022003725](RabbitMQ.assets/image-20240225022003725.png)

Observe que diferentemente do teste realizado em "*Entendendo o Problema*¨, não foi enviado somente para o primeiro *consumer*, mas sim enviou dois para cada um dos *consumers* até o **Round Robin** se iniciar.

---

## 8. Exchange

O protocolo **AMQP** visa padronizar a interação entre clientes e o *middleware* de "*mensageria*" os famosos ***brokers***. Para entender como o protocolo funciona é necessário conhecer seus componentes e como se dá a integração entre eles.

![AMQP 0-9-1 Model Explained | RabbitMQ](RabbitMQ.assets/hello-world-example-routing-cbe9a872b37956a4072a5e13f9d76e7b.png)

O *middleware* de mensagem pode ser definido como um servidor de dados que recebe mensagens e faz basicamente duas coisas com elas: as encaminha para diferentes *consumers* dependendo da política de rotas ou as armazena, em memória ou disco, quando os *consumers* não são capazes de aceitá-las imediatamente. 

Uma das vantagens entre soluções que implementam o protocolo **AMQP** e as que não implementam o protocolo são os componentes. Os componentes criados pelo protocolo são modulares e tem responsabilidades bem definidas, por exemplo:

- **Exchange**: tem como tarefa de receber as mensagens do *producer* e encaminhar as mensagens para filas;
- **Queues**: armazena as mensagens e encaminham elas para os *consumers*.

Componentes com responsabilidades bem definidas implicam em reutilização, por exemplo, é possível usar uma mesma *exchange* para abastecer diferentes filas (*queues*) que, podem abastecer diferentes *consumers* e produzir resultados diferentes. Esta característica aliada a capacidade do protocolo de criar *exchanges*, filas (*queues*) de mensagem e associa-las em tempo de execução torna possível construir qualquer sistema baseado em mensagens.

Existem alguns tipos de *exchanges*, e veremos cada um deles a seguir.

### 8.1. Default Exchange

Esse é o *exchange* padrão fornecida pelo ***broker***, e não possui nome. Apesar disso, tem uma característica muito interessante: toda fila criada é automaticamente associada a ela com uma rota que é igual ao nome da fila. Em miúdos, se eu declarar uma fila com o nome ***foo*** e publicar uma mensagem no ***default exchange*** com a rota ***foo***, esta mensagem será entregue na fila de mesmo nome, dando a impressão que estou me comunicando diretamente com a fila.

### 8.2. Fanout Exchange

Este *exchange* simplesmente ignora a rota. Seu comportamento resume-se em mandar uma cópia das mensagens para todas as filas que estão associadas a ele.

IMAGEM

Acima, o produtor avisa a mensagem para o *exchange* (do tipo *fanout*), que por sua vez repassa uma cópia da mensagem para cada uma das filas vinculadas ao *exchange*.

Abaixo criamos um *producer* utilizando o *exchange fanout*:

```csharp
var exchange = "order_fanout";
var counter = 0;

// Define um factory connection: informações para conexão com o servidor RabbitMQ
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "ellison",
    Password = "Abcd@1234",
    Port = 5672
};

using var connection = factory.CreateConnection();

var channel = SetupChannel(connection, exchange, "order", "logs", "finance");

while(true)
{
    for (int i = 0; i < 10; i++)
        PublishMessage(channel, exchange, $"{counter++}");
    
    Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
    Console.ReadLine();
}

static void PublishMessage(IModel channel, string exchange, string message)
{
    var body = Encoding.UTF8.GetBytes(message);

    // Publish in exchange
    channel.BasicPublish(exchange, string.Empty, null, body);
}

static IModel SetupChannel(IConnection connection, string enchange, params string[] queues)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define queue
    foreach(var queue in queues)
        channel.QueueDeclare(queue, false, false, false, null);

    // Define exchange
    channel.ExchangeDeclare(enchange, "fanout");

    // Bind queue
    foreach(var queue in queues)
        channel.QueueBind(queue, enchange, string.Empty);

    return channel;
}
```

Inicialmente, precisamos configurar o *channel* (presente no método `SetupChannel`):

- Declarando quais filas farão parte do *exchange*;
- Declarando o *exchange*. Nesse caso, informando que se trata de um *exchange fanout*;
- Vinculando todas as filas ao *exchange* (*binding*).

Além disso, muda também na publicação: ao invés de publicar na fila, precisamos somente passar o *exchange* ao qual queremos publicar (presente no método `PublishMessage`).

### 8.3. Direct Exchange

O comportamento do *direct exchange* é de encaminhar mensagens que possuam exatamente a mesma rota (*routing key*) das filas associadas a este *exchange*. Abaixo uma ilustração de como funciona:

![image-20240225213146003](RabbitMQ.assets/image-20240225213146003.png)

Observe que o *producer* encaminha a mensagem ao *exchange direct* e a partir dele existem duas filas associadas:

- `Finance_Order` que para ser encaminhada a ela é necessário que a mensagem venha com a rota (*routing key*) igual a `order_new`;
- `Order` que para ser encaminhada a ela é necessário que a mensagem venha com algumas das rotas (*routing key*) `order_new` ou `order_upd`;

Logo, se uma mensagem for enviada ao *exchange* somente com o *routing key* `order_upd`, essa mensagem será repassada somente para a fila `Order`, já que somente ela assina a *routing key* `order_upd`. Se a mensagem for enviada ao *exchange* com o *routing key* `order_new`, ela será enviada a todas as filas que assinam o *routing key* `order_new`, ou seja, tanto a fila `Finance_Order` quanto a `Order`.

Abaixo veremos um exemplo de implementação do *exchange direct*: 

```csharp
var counter = 0;

var exchange = "order_direct";

const string ORDER_QUEUE = "order";
const string FINANCE_ORDER_QUEUE = "finance_order";
const string ORDER_NEW_RK = "order_new";
const string ORDER_UPD_RK = "order_upd";

// Define um factory connection: informações para conexão com o servidor RabbitMQ
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "ellison",
    Password = "Abcd@1234",
    Port = 5672
};

using var connection = factory.CreateConnection();

var channel = SetupChannel(connection, exchange);

while(true)
{
    for (int i = 0; i < 10; i++)
        PublishMessage(channel, exchange, ORDER_NEW_RK, $"{counter++}");
    
    Console.WriteLine("Pressionar [ENTER] to send 10 messages...");
    Console.ReadLine();
}

void PublishMessage(IModel channel, string exchange, string routingKey, string message)
{
    var body = Encoding.UTF8.GetBytes(message);

    // Publish in exchange (with routing key)
    channel.BasicPublish(exchange, routingKey, null, body);
}

IModel SetupChannel(IConnection connection, string exchange)
{
    // Create channel
    var channel = connection.CreateModel();

    // Define queue
    channel.QueueDeclare(ORDER_QUEUE, false, false, false, null);
    channel.QueueDeclare(FINANCE_ORDER_QUEUE, false, false, false, null);

    // Define exchange
    channel.ExchangeDeclare(exchange, "direct");

    // Bind queue
    channel.QueueBind(ORDER_QUEUE, exchange, ORDER_NEW_RK);
    channel.QueueBind(ORDER_QUEUE, exchange, ORDER_UPD_RK);
    channel.QueueBind(FINANCE_ORDER_QUEUE, exchange, ORDER_NEW_RK);

    return channel;
}
```

Observe a configuração do *channel* (no método `SetupChannel`):

- Declaramos as filas como anteriormente;
- Declaramos o *exchange*. Observe que informamos que é do tipo *direct*;
- Vinculamos a fila ao *exchange* e as respectivas *routing keys*. Observe que a fila `Order` contém dois *binding* já que ela assina os dois *routing key*.

Além disso, na publicação (no método `PublishMessage`), precisamos indicar qual  o *exchange* e também com qual *routing key* que queremos publicar a mensagem.

### 8.4. Topic Exchange

Este *exchange* encaminha mensagens de acordo com a rota definida na mensagem e o padrão definido na associação da fila ao *exchange*. A rota é definida através do *routing key*, e funciona como um *RegEx*.

Supomos que temos uma empresa de *finance*, onde existe o núcleo **Bahia** e **Rio de Janeiro**, e cada um dos núcleos contém o setor de logística (**LG**) e recursos humanos (**RH**). Dessa forma a arvore ficaria da seguinte forma:

![image-20240225222503498](RabbitMQ.assets/image-20240225222503498.png)

Supomos também que cada núcleo vira uma fila e que cada fila receba as mensagens dos seus filhos, ou seja, se o setor de **recursos humanos** (**RH**) da **Bahia** (**BA**) recebe uma mensagem, essa mensagem também deve chegar na fila **BA**, e também chegará na fila ***finance***.

Isso é possível através do *exchange topic*, indicando o nível a ser acessado através do *routing key*, onde cada nível é separado por ponto (.). Por exemplo:

- Na fila `finance_ba_lg` precisar estar na *routing key* `finance.ba.lg`;
- Na fila `finance_ba_rh` precisar estar na *routing key* `finance.ba.rh`;
- Na fila `finance_rj_lg` precisar estar na *routing key* `finance.rj.lg`;
- Na fila `finance_rj_rh` precisar estar na *routing key* `finance.rj.rh`;
- Na fila `finance_ba` precisa estar na *routing key* `finance.ba`, caso seja uma mensagem direto para ela, e também a *rk* `finance.ba.*`, para aceitar qualquer mensagem do nível seguinte a `finance.ba`; 
- Na fila `finance_rj` precisa estar na *routing key* `finance.rj`, caso seja uma mensagem direto para ela, e também a *rk* `finance.rj.*`, para aceitar qualquer mensagem do nível seguinte a `finance.rj`;
- Na fila `finance` precisa estar na *routing key* `finance` e `finance.#`, dessa forma (**usando o #**) ela recebe mensagem de todos os níveis e subníveis abaixo dela.

É muito importante ressaltar que os padrões tem duas regras básicas: O asterisco (*), que substitui apenas UMA palavra (um nível). Se você quiser substituir zero ou mais (assim como o operador * da *RegEx*) você precisa usar o **#**. No caso acima, somente a fila `finance` obtém todas as mensagens dos filhos, por isso o `finance.#`.

Um último caso de ser analisado: e se for criado um setor de RH global, que administre todos os RHs de todos os núcleos (e respectivamente a fila `finance_rh`). Para isso, se resolve somente com a *routing key* `finance.*.rh`

Abaixo, temos a implementação de toda a configuração do *exchange* com os respectivos *routing key*:

```csharp
public class Program 
{   
    public static void Main(string[] args)
    {
        var counter = 0;
        var exchange = "order_topic";

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "ellison",
            Password = "Abcd@1234",
            Port = 5672
        };

        using var connection = factory.CreateConnection();

        var channel = SetupChannel(connection, exchange);

        while(true)
        {
            foreach (var rk in args)
                for (int i = 0; i < 10; i++)
                    PublishMessage(channel, exchange, rk, 
                                   $"{counter++}");
            
            Console.WriteLine(
                "Pressionar [ENTER] to send 10 messages...");
            Console.ReadLine();
        }
    }

    static void PublishMessage(IModel channel, string exchange, string routingKey, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        // Publish in queue
        channel.BasicPublish(exchange, routingKey, null, body);
    }

    static IModel SetupChannel(IConnection connection, string exchange)
    {
        // Create channel
        var channel = connection.CreateModel();

        // Define queue
        channel.QueueDeclare("finance", false, false, false, null);
        channel.QueueDeclare("finance_ba", false, false, false, null);
        channel.QueueDeclare("finance_ba_lg", false, false, false, null);
        channel.QueueDeclare("finance_ba_rh", false, false, false, null);
        channel.QueueDeclare("finance_rj", false, false, false, null);
        channel.QueueDeclare("finance_rj_lg", false, false, false, null);
        channel.QueueDeclare("finance_rj_rh", false, false, false, null);
        channel.QueueDeclare("finance_rh", false, false, false, null);

        // Define exchange
        channel.ExchangeDeclare("order_topic", "topic");

        // Bind queue
        channel.QueueBind("finance", exchange, "finance");
        channel.QueueBind("finance", exchange, "finance.#");
        channel.QueueBind("finance_ba", exchange, "finance.ba");
        channel.QueueBind("finance_ba", exchange, "finance.ba.*");
        channel.QueueBind("finance_ba_lg", exchange, "finance.ba.lg");
        channel.QueueBind("finance_ba_rh", exchange, "finance.ba.rh");
        channel.QueueBind("finance_rj", exchange, "finance.rj");
        channel.QueueBind("finance_rj", exchange, "finance.rj.*");
        channel.QueueBind("finance_rj_lg", exchange, "finance.rj.lg");
        channel.QueueBind("finance_rj_rh", exchange, "finance.rj.rh");
        channel.QueueBind("finance_rh", exchange, "finance.*.rh");

        return channel;
    }
}
```

### 8.5. Headers Exchange

Esse é o tipo menos comum de *exchange*. Este tipo ignora a rota e encaminha as mensagens usando seu cabeçalho. Então se você possui uma condição mais complexa que uma `string` para encaminhar a mensagem, você pode fazer uso deste tipo de *exchange*. Ele funciona BEM mais ou menos como um `if`, já que é possível definir mais de um critério para encaminhamento da mensagem.

Abaixo um exemplo no qual criei quatro filas, a `logistics`, `accounting`, `finance` e `auditorship`. Onde foi definido um *header* de nome `section` para todas elas com os respectivos valores para o *header* `logistics`, `accouting`, `finance` e `finance`, ou seja, tanto a fila `finance` quanto `auditorship` responde ao *header* `section` como "*finance*".

Veja abaixo um exemplo de implementação:

```csharp
public class Program 
{   
    public static void Main(string[] args)
    {
        var counter = 0;
        var exchange = "order_header";

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            UserName = "ellison",
            Password = "Abcd@1234",
            Port = 5672
        };

        using var connection = factory.CreateConnection();

        var channel = SetupChannel(connection, exchange);

        while(true)
        {
            foreach (var header in args)
                for (int i = 0; i < 10; i++)
                    PublishMessage(channel, 
                                   exchange, 
                                   header, 
                                   $"{counter++}");
            
            Console.WriteLine(
                "Pressionar [ENTER] to send 10 messages...");
            Console.ReadLine();
        }
    }

    static void PublishMessage(IModel channel, 
                               string exchange, 
                               string headerValue, 
                               string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        // Publish in exchange with header
        var headers = new Dictionary<string, object> 
        { 
            {"section", headerValue} 
        };

        var basicProperties = channel.CreateBasicProperties();
        basicProperties.Headers = headers;

        channel.BasicPublish(exchange, 
                             string.Empty, 
                             basicProperties, 
                             body);
    }

    static IModel SetupChannel(IConnection connection, 
                               string exchange)
    {
        // Create channel
        var channel = connection.CreateModel();

        // Define queue
        channel.QueueDeclare("logistics", false, false, false, null);
        channel.QueueDeclare("accounting", false, false, false, null);
        channel.QueueDeclare("finance", false, false, false, null);
        channel.QueueDeclare("auditorship", false, false, false, null);

        // Define exchange
        channel.ExchangeDeclare(exchange, "headers");

        // Bind queue
        var financeHeaders = new Dictionary <string, object> 
        { 
            {"section", "finance"} 
        };
        channel.QueueBind(
            "finance", 
            exchange, 
            string.Empty, 
            financeHeaders);
        
        var accountingHeaders = new Dictionary<string, object> 
        { 
            {"section", "accounting"} 
        };
        channel.QueueBind("accounting", 
                          exchange, 
                          string.Empty, 
                          accountingHeaders);
        
        var logisticsHeaders = new Dictionary<string, object> 
        { 
            {"section", "logistics"} 
        };
        channel.QueueBind("logistics", 
                          exchange, 
                          string.Empty, 
                          logisticsHeaders);

        var auditorshipHeaders = new Dictionary<string, object> 
        { 
            {"section", "finance"}
        };
        channel.QueueBind("auditorship", 
                          exchange, 
                          string.Empty, 
                          auditorshipHeaders);

        return channel;
    }
}
```

Observe que no `SetupChannel`, configuramos: 

- Declaramos as filas `logistics`, `accounting`, `finance` e `auditorship`;
- Declaramos o *exchange headers*;
- Fazemos o *bind* da fila para o *exchange* com os *headers* para cada uma das filas. Nesse caso, só incluímos o *header* `section`.

Dessa forma, em `PublishMessage`, observamos que para publicar basta informar o ***exchange*, a mensagem e os *headers***. Dessa forma, o *exchange* vai entender os *headers* da mensagem e vai repassar para a fila que contém o mesmo *header name* e *header value*.

---

## 9. Message Looping

Sabemos que quando o *autoack* está ativado, caso ocorra erro durante a leitura da mensagem, ela é reenviada a fila para ser processada novamente. Com isso, caso não seja processada, acontecerá um *message looping*, ou seja, ao reprocessar será reenviada para a fila e novamente voltará a ser reprocessada em *looping*.

Um *message looping* pode gerar um aumento no consumo de memória e de processamento do **RabbitMQ**. Dado isso, temos algumas estratégias de como resolver e abaixo veremos a ***Dead Letter***.

### 9.1 Dead Letter

O ***Dead Letter*** é uma estratégia para evitar o *message looping* que consiste em: 

1. O *consumer* tenta ler a mensagem; 
2. No processamento da mensagem ocorre um erro e indicamos um *nack* (que a mensagem não foi processada);
3. Ao invés dela voltar para fila de origem, ela é reenviada a um *exchange* de ***Dead Letter*** que repassará para uma fila específica de mensagens que não conseguiram ser processadas.

Observe o esquema acima descrito na imagem abaixo: 

![What is RabbitMQ Dead Letter Exchange and How to Configure it - Tutexchange](RabbitMQ.assets/image.png)

Não é preciso realizar todos os passos descritos acima na mão, existe uma configuracão no próprio **RabbitMQ** (configuracão feita na fila) que indica que qualquer mensagem retornada para a fila deve ser enviada a um *exchange* de ***Dead Letter***.

Abaixo temos uma implementacão básica do ***Dead Letter*** em .NET onde temos:

- Uma fila de nome `order`, que contém a configuracão de ***Dead Letter***;
- Um *exchange* `DeadLetterExchange` e sua respectiva fila `DeadLetterQueue` que receberá todas as mensagens de `order` que não conseguiram ser processadas.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory() {
            Uri = new Uri("amqps://ijtvydws:dBbJc7kuCC0ulXc5NW6b0L-iUyphn1nQ@jackal.rmq.cloudamqp.com/ijtvydws")
        };

        using var connection = factory.CreateConnection();
        var channel = SetupChannel(connection);

        CreateConsumer(channel);

        Console.WriteLine("Press [ENTER] to exit...");
        Console.ReadLine();
    }

    private static IModel SetupChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.ExchangeDeclare("DeadLetterExchange", ExchangeType.Fanout);
        channel.QueueDeclare("DeadLetterQueue", false, false, false, null);
        channel.QueueBind("DeadLetterQueue", "DeadLetterExchange", string.Empty);

        var arguments = new Dictionary<string, object>() { {"x-dead-letter-exchange", "DeadLetterExchange"} };

        channel.QueueDeclare("order", false, false, false, arguments); 
        channel.BasicQos(0, 1, false);

        return channel;
    }

    private static void CreateConsumer(IModel channel)
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (sender, ea) => {
            try 
            {
                var body = ea.Body.Span;
                var message = Encoding.UTF8.GetString(body);

                var number = int.Parse(message);

                Console.WriteLine($"[Success]\t{number}");

                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Failed]\t{ex.Message}");
                channel.BasicNack(ea.DeliveryTag, false, false);
            }
        };

        channel.BasicConsume("order", false, consumer);
    }
}
```

A configuracão do *DeadLetter* está dentro do método `SetupChannel`: 

- Declaramos o *exchange* `DeadLetterExchange`, a fila `DeadLetterQueue` e fizemos o *binding* deles;
- Declaramos a fila `order` e fazemos a configuracão para o *Dead Letter*: criamos um argumento para a fila de nome `x-dead-letter-exchange`, onde seu valor é o nome do *exchange* que deverá ir a mensagem, que nesse caso é o *exchange* `DeadLetterExchange`.

> A configuracão do argumento `x-dead-letter-exchange` na fila é o suficiente para o **RabbitMQ** entender que a mensagem que não conseguiu ser processada seja enviada para o *exchange*. Não é necessário o envio manual.
>
> Abaixo é possível observar no *manager* que a fila recebe uma *feature* de nome **DLX**, que é o *dead letter*.
>
> ![image-20240217173106309](RabbitMQ.assets/image-20240217173106309.png)

> Para que ela não seja reprocessada, no momento de consumir é necessário que no *nack* (no código) o *requeue* esteja como falso.

---

## 10. Time to Live (TTL)

É possível, no **RabbitMQ** definir o **TTL** (Time To Live) das mensagens, ou seja, definir um tempo para que elas permaneçam na fila, e caso esse tempo se esgote, a mensagem é excluída permanentemente. Essa configuracão pode ser feita de duas formas: 

- Através de cada mensagem: dessa forma podendo definir **TTL** (diferentes ou não) para cada uma das mensagens;
- Através da fila: onde toda mensagem daquela fila terá o mesmo **TTL** definido pela fila.

### 10.1. TTL da mensagem

Para definir o **TTL** da mensagem, em **.NET**, criamos um *Producer* de exemplo: 

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory 
        {
            Uri = new Uri("amqps://ijtvydws:dBbJc7kuCC0ulXc5NW6b0L-iUyphn1nQ@jackal.rmq.cloudamqp.com/ijtvydws")
        };

        using var connection = factory.CreateConnection();
        
        var channel = CreateChannel(connection);

        PublishMessage(channel, Guid.NewGuid().ToString());
    }

    private static IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();
        channel.QueueDeclare("order", false, false, false, null);
        return channel;
    }

    private static void PublishMessage(IModel channel, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        var props = channel.CreateBasicProperties();

        props.Expiration = "20000";

        channel.BasicPublish(string.Empty, "order", props, body);
    }
}

```

Podemos focar a configuracão do **TTL** no método `PublishMessage`:

- Criamos um `IBasicProperties` com o `channel.CreateBasicProperties()`;
- Colocamos na propriedade `Expiration` do `IBasicProperties` o tempo de **TTL** em milissegundos `20000`;
- Finalmente, inserimos o `IBasicProperties` `props` no `BasicPublish` para publicar a mensagem junto com a configuracão da propriedade de **TTL**.  

Ao executar esse código é possível observar que a configuracão fica nas **propriedades da mensagem** (ao dar um *Get Message(s)*), como abaixo: 

![image-20240217180310613](RabbitMQ.assets/image-20240217180310613.png)

> Usar **TTL** na mensagem é interessante quando é necessário dar um **TTL** específico para cada mensagem. Caso contrário, é uma alternativa melhor definir **TTL** na fila.

### 10.2. TTL na fila

Para definir o **TTL** da fila, em **.NET**, criamos um *Producer* de exemplo: 

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory 
        {
            Uri = new Uri("amqps://ijtvydws:dBbJc7kuCC0ulXc5NW6b0L-iUyphn1nQ@jackal.rmq.cloudamqp.com/ijtvydws")
        };

        using var connection = factory.CreateConnection();
        
        var channel = CreateChannel(connection);

        PublishMessage(channel, Guid.NewGuid().ToString());
    }

    private static IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        var args = new Dictionary<string, object>
        {
            { "x-message-ttl", 22000 }
        };

        channel.QueueDeclare("order", false, false, false, args);
        
        return channel;
    }

    private static void PublishMessage(IModel channel, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        channel.BasicPublish(string.Empty, "order", null, body);
    }
}
```

Podemos focar a configuracão do **TTL** no método `CreateChannel`:

- Criamos um dicionário de argumentos `args` (assim como na configuracão de *dead letter*), com o parâmetro de nome `x-message-ttl` e valor igual ao tempo **TTL** em milissegundos;
- E informamos esse dicionário `args` a declaracão da fila `order`.

Ao executar esse código é possível observar que a configuracão fica nas ***features* da fila**, como abaixo: 

![image-20240217180948951](RabbitMQ.assets/image-20240217180948951.png)

![image-20240217181015814](RabbitMQ.assets/image-20240217181015814.png)

---

## 11. Durable Queue e Persistence Message

Até o momento, sempre que desligamos o *container* tanto a fila quanto a mensagem é excluída. Para que isso não aconteça, devemos configurar duas coisas: 

-  O *durable queue*, que indica se a fila é excluída ou não após o desligamento do container de **RabbitMQ**;
- O *persistence message*, que indica que a mensagem deve ser persistida em disco.

### 11.1. Durable Queue

Para definirmos que uma fila é durável é simples: basta indicar no `QueueDeclare` no parâmetro `durable` igual a `true`. Com isso, toda vez que o *container* for reiniciado, a fila continuará lá. 

> Indicar a fila como durável não significa que a mensagem também será durável, ou seja, se a fila for indicada como durável, mas a mensagem não for persistida, mesmo assim as mensagens serão excluídas.

Quando a fila é marcada com durável, exibe um `D` nas *features*:

![image-20240218015055753](RabbitMQ.assets/image-20240218015055753.png)

### 11.2. Persistence Message

Como indicamos anteriormente, se a fila for indicada como durável somente, as mensagens continuarão sendo excluídas ao desligar/reiniciar o *broker*. Para que as mensagens não sejam perdidas, é necessário persisti-las. Para isso: 

- É necessário criarmos um `IBasicProperties` com `channel.CreateBasicProperties()`, e indicarmos a propriedade `Persistent` como `true`;
- Passamos esse `IBasicProperties` `props` na publicacão da mensagem (no `BasicPublish`, no parâmetro `basicProperties`). 

Abaixo podemos ver um exemplo do código implementando o *durable queue* e o *persistence message*:

```csharp
public static void Main(string[] args)
{
    var factory = new ConnectionFactory
    {
        HostName = "localhost",
        Port = 5672
    };

    using var connection = factory.CreateConnection();

    var channel = connection.CreateModel();

    channel.QueueDeclare(queue: "order", 
        durable: true, 
        exclusive: false, 
        autoDelete: false, 
        arguments: null);

    var message = Guid.NewGuid().ToString();
    var body = Encoding.UTF8.GetBytes(message);

    var props = channel.CreateBasicProperties();

    props.Persistent = true;

    channel.BasicPublish(
        exchange: string.Empty, 
        routingKey: "order", 
        basicProperties: props, 
        body: body);        
}
```

> Quando definimos `Persistent = true` indicamos que a mensagem agora será persistida em disco e automaticamente nos leva a pensar em problemas gerados, como a quantidade de memória disponível para isto e a perda de performance por causa da persistência da mensagem.

---

## 12. Publish Confirmation e Return Message

Até o momento, nossas publicacões de mensagens consiste em mandarmos para a fila, passando pelo *channel* e pelo *exchange*. Conforme imagem abaixo: 

![image-20240218193605189](RabbitMQ.assets/image-20240218193605189.png)

Dessa forma, se a mensagem realmente vai para a fila ou não (se foi aceita ou não pelo *broker*), não conseguimos ter essa informação, muito menos a possibilidade de manipular ou tratar. 

Abaixo serão apresentadas algumas técnicas que possibilita o monitoramento pós publicação da mensagem.

### 12.1. Publish Confirmation

Conseguimos através do **RabbitMQ** indicar métodos de *callback* para quando a mensagem é aceita (*ack*) ou rejeitada (*nack*). Conforme ilustracão abaixo: 

![image-20240218195531921](RabbitMQ.assets/image-20240218195531921.png)

Para isso, precisamos configurar duas coisas: 

- Precisamos informar para o *channel* que iremos trabalhar com a confirmacão de mensagem, ou seja, indicar `channel.ConfirmSelect()`. A partir disso, é possível indicar os métodos de *callback*;
- Indicar os métodos de *callback* para `channel.BasicAcks` e para `channel.BasicNacks`.

Essas configuracões foram feitas no código abaixo:

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory 
        {
            HostName = "localhost",
            Port = 5672
        };

        using var connection = factory.CreateConnection();

        var channel = CreateChannel(connection);

        var message = Guid.NewGuid().ToString();

        PublishMessage(channel, message);

        Console.WriteLine("Press [ENTER] to exit...");
        Console.ReadLine();
    }

    private static void PublishMessage(IModel channel, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "", 
            routingKey: "order", 
            mandatory: false, 
            basicProperties: null, 
            body: body);
        
        channel.WaitForConfirms(new TimeSpan(0, 0, 10));
    }

    private static IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.ConfirmSelect();

        channel.BasicAcks += Channel_BasicAcks;
        channel.BasicNacks += Channel_BasicNacks;

        channel.QueueDeclare("order", false, false, false, null);

        return channel;
    }

    private static void Channel_BasicNacks(
        object? sender, BasicNackEventArgs e)
    {
        Console.WriteLine($"[X] Nack: {DateTime.Now:d}");
    }

    private static void Channel_BasicAcks(
        object? sender, BasicAckEventArgs e)
    {
        Console.WriteLine($"[O] Ack: {DateTime.Now:d}");
    }
}

```

Dessa forma, `Channel_BasicAcks` será chamada se a mensagem for aceita pelo *broker* e a `Channel_BasicNacks` quando rejeitada. 

Podemos ainda, obrigar o código a esperar a confirmacão (seja *ack* ou *nack*) por determinado tempo, inserindo `channel.WaitForConfirms(Timespan timespan)` após a publicacão (`BasicPublish`). Caso a mensagem não seja confirmada durante este tempo, será lancada uma *exception*.

> Usar o `channel.WaitForConfirms` é interessante, pois pode ocorrer de enviar uma mensagem no qual o *broker* não consegue entender pra onde a encaminhar. Ocorre por exemplo: quando o nome do *exchange* de publicacão não condiz com nenhum no **RabbitMQ**.

### 12.2. Return Message

O *nacks* do tópico anterior só ocorre realmente quando ocorre um error no *broker* ao tentar colocar a mensagem na fila. 

Um exemplo clássico, é que se o *exchange* informado no momento da publicacão existe, porém, a fila associada no *routingKey* não, não ocorrerá *exception*, pois será retornado um *ack* da mesma forma, já que o *exchange* foi encontrado. Desta forma, temos um problema e a solução até então não protege deste acontecimento.

Para isso, podemos utilizar o *return message* para verificar se o **RabbitMQ** deu conta do *binding* da mensagem: podemos criar um método de *callback* (evento) que será executado caso o **RabbitMQ** não conseguir achar a fila correta, ou não achar um caminho para mensagem do início até o colocar numa fila. Resumindo, é um método que é executado quando o **RabbitMQ** não consegue fazer o processamento da mensagem.

Para configurar esse método:

- Indicar, assim como no `BasicAcks` e `BasicNacks`, o `BasicRuturn` no *channel*. É possível observar no código abaixo dentro do método `CreateChannel`;
- Indicar na publicacão, ou seja, no `BasicPublish`, a *flag* `mandatory: true`.

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory 
        {
            HostName = "localhost",
            Port = 5672
        };

        using var connection = factory.CreateConnection();

        var channel = CreateChannel(connection);

        var message = Guid.NewGuid().ToString();

        PublishMessage(channel, message);

        Console.WriteLine("Press [ENTER] to exit...");
        Console.ReadLine();
    }

    private static void PublishMessage(IModel channel, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(
            exchange: "", 
            routingKey: "orderr", 
            mandatory: true, 
            basicProperties: null, 
            body: body);

        channel.WaitForConfirms(new TimeSpan(0, 0, 10));
    }

    private static IModel CreateChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.ConfirmSelect();

        channel.BasicAcks += Channel_BasicAcks;
        channel.BasicNacks += Channel_BasicNacks;
        channel.BasicReturn += Channel_BasicReturn;

        channel.QueueDeclare("order", false, false, false, null);

        return channel;
    }

    private static void Channel_BasicReturn(
        object? sender, BasicReturnEventArgs e)
    {
        Console.WriteLine($"[*] Return: {DateTime.Now:d}");
    }

    private static void Channel_BasicNacks(
        object? sender, BasicNackEventArgs e)
    {
        Console.WriteLine($"[X] Nack: {DateTime.Now:d}");
    }

    private static void Channel_BasicAcks(
        object? sender, BasicAckEventArgs e)
    {
        Console.WriteLine($"[O] Ack: {DateTime.Now:d}");
    }
}
```

Observe que nesse código, colocamos o nome da fila (`routingKey`) errada para replicarmos o problema descrito. Precisamos nos atentar também, que o *exchange* existe (*default*). Dessa forma:

- O *ack* continua sendo executado, já que foi uma mensagem aceita pelo *exchange*;
- O *return* é executado já que o *broker* não consegue processar a mensagem, ou seja, não consegue entender para onde enviar.

Abaixo o resultado do código acima:

![image-20240218215713037](RabbitMQ.assets/image-20240218215713037.png)

---

## 13. Remote Procedure Call (RPC)

Até o momento, estamos criando *producers* e *consumers* separadamente, onde o *producer* produz uma mensagem para uma fila e existe um *consumer* escutando a fila e realizando as devidas ações. 

Em muitos casos o *producer* também espera retorno do *consumer*, ou seja, o *producer* produz uma mensagem, que contém um identificador e a fila de retorno, e o próprio *producer* espera nessa fila de retorno. No servidor o retorno ele escuta a fila, ler a mensagem, processa e joga na fila de retorno para que o *producer* que o enviou, receba o *callback* do processamento dessa mensagem. 

O caso mencionado acima, se chama **Remote Procedure Call (RPC)**, quando uma aplicação produz uma mensagem numa fila e espera o retorno do processamento (do *consumer* que leu), e a aplicação que está lendo essa fila, ler a mensagem e retorna o resultado do processamento para a fila de retorno (indicada por quem mandou a mensagem).

![image-20240221233318658](RabbitMQ.assets/image-20240221233318658.png)

Na imagem acima, iremos imaginar que a mensagem saia do **Processor** e é enviada para a fila **Incomming Queue**. Essa mensagem enviada contém os seguintes parâmetros:

- *Reply queue*, que se trata do nome da fila para retornar o processamento da mensagem;
- *CorrelationId*, que identifica que a mensagem partiu daquele **Processor**.

Após isso, o **Processor2** ler a fila **Incomming Queue**, obtém e processa a mensagem e devolve para a fila de retorno **Reply Queue** (indicada na mensagem), contendo o mesmo *correlation id*. 

Finalmente o **Processor** obtém a resposta dessa fila de retorno e a trata. Abaixo vamos entender no código.

### 13.1. Entendendo o Client

O *client* será a aplicação que enviará a mensagem, contendo as propriedades *reply queue* e *correlation id*, e escutará na fila *reply queue* o retorno do processamento do servidor.

```csharp

public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory 
        { 
            HostName = "localhost",
            Port = 5672
        };

        using var connection = factory.CreateConnection();
        var channel = SetupChannel(connection);

        var correlationId = Guid.NewGuid().ToString();

        InitializerConsumer(channel, correlationId);

        var props = channel.CreateBasicProperties();
        props.CorrelationId = correlationId;
        props.ReplyTo = $"{nameof(Order)}_return";

        while (true)
        {
            Console.Write("Valor do pedido: ");

            var amount = decimal.Parse(Console.ReadLine()!);

            var order = new Order(amount);
            var message = JsonSerializer.Serialize(order);
            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(string.Empty, nameof(Order), props, body);

            Console.WriteLine($"Published: {message}");
            Console.ReadLine();
            Console.Clear();
        }
    }

    private static IModel SetupChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: $"{nameof(Order)}_return",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );
        
        channel.QueueDeclare(
            queue: nameof(Order),
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        return channel;
    }

    private static void InitializerConsumer(IModel channel, string correlationId)
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) => 
        {
            if (correlationId.Equals(ea.BasicProperties.CorrelationId))
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"Received: {message}");

                return;
            }

            Console.WriteLine($"Mensagem descartada, identificadores de correlacão inválidos. Original: {correlationId}, Recebido: {ea.BasicProperties.CorrelationId}");
        };

        channel.BasicConsume(
            queue: $"{nameof(Order)}_return",
            autoAck: true,
            consumer: consumer
        );
    }
}
```

Observações desse código:

1. Inicialmente definimos um *correlationId* para identificar as mensagens publicadas por essa aplicação;
2. Criamos um *consumer* para a fila de retorno `Order_return` no método `InitializerConsumer` para esperar o retorno do processamento das mensagens no servidor;
3. Após criar o *consumer*, definimos propriedades para a mensagem do publicador, como dito anteriormente, associando um: 
   1. *Correlation id*;
   2. *Reply to* contendo a fila de retorno, nesse caso, a fila `Order_return`.
4. Finalmente, fazemos um *looping* para publicação, serializando a mensagem `Order` e enviando a mensagem com as propriedades definidas.

### 13.2. Entendendo o Server

O *server* será a aplicação que escutará a fila `Order`, e assim que escutar a mensagem, ela vai devolver na fila de retorno (que está nas propriedades da mensagem).

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672
        };

        using var connection = factory.CreateConnection();
        var channel = SetupChannel(connection);

        InitializerConsumer(channel);

        Console.WriteLine("Press [ENTER] to exit...");
        Console.ReadLine();
    }
    
    private static IModel SetupChannel(IConnection connection)
    {
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: nameof(Order),
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        channel.BasicQos(0, 1, false);

        return channel;
    }

    private static void InitializerConsumer(IModel channel)
    {
        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            try
            {
                var incommingMessage = Encoding.UTF8.GetString(ea.Body.ToArray());
                Console.WriteLine($"{DateTime.Now:o} Incomming -> {incommingMessage}");

                var order = JsonSerializer.Deserialize<Order>(incommingMessage);
                order!.SetStatus(ProccessOrderStatus(order.Amount));

                var replyMessage = JsonSerializer.Serialize(order);
                Console.WriteLine($"{DateTime.Now:o} Reply -> {replyMessage}");

                SendReplyMessage(replyMessage, channel, ea);

                channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            }
            catch (Exception)
            {
                // Efetuar log
                // Pode também retornar mensagem com status de erro.
            }
        };

        channel.BasicConsume(
            queue: nameof(Order),
            autoAck: false,
            consumer: consumer
        );
    }

    private static void SendReplyMessage(string replyMessage, IModel channel, BasicDeliverEventArgs ea)
    {
        var props = ea.BasicProperties;
        var replyProps = channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;

        var responseBytes = Encoding.UTF8.GetBytes(replyMessage);

        channel.BasicPublish(string.Empty, props.ReplyTo, replyProps, responseBytes);
    }

    private static OrderStatus ProccessOrderStatus(decimal amount)
    {
        return OrderServices.OnStore(amount);
    }
}
```

Com isso:

1. Definimos na um *consumer* para a fila `Order`; 
2. Observe que esse *consumer*:
   1. Deserializa a mensagem;
   2. Faz o processamento da mensagem em `ProccessOrderStatus` e seta o novo status da mensagem;
   3. Serializa a mensagem e manda para a fila de retorno em `SendReplyMessage`.
3. No `SendReplyMessage` observe que obtemos as propriedades da mensagem recebida, criando propriedades da mensagem que será enviada e associando o mesmo `CorrelationId`. Finalmente, publicamos a mensagem na fila de retorno com a propriedade.

