module LSharp.Rabbit

open System
open RabbitMQ
open RabbitMQ.Client
open RabbitMQ.Client.Events
open System.Collections.Generic
open System.Text
open System.Text.Json

type QueueDeclareOpts = {
    Name: string;
    Durable: bool;
    Exclisive: bool;
    AutoDelete: bool;
    Args: Map<string, obj>
}

type PublishOpts = {
    Exchange: string;
    Routing: string;
    Properties: IBasicProperties;
    Body: byte array;
}

let private mapToDict (map: Map<'a, 'b>) =
    map
    |> Map.toSeq
    |> Seq.map (fun pair -> KeyValuePair(fst pair, snd pair))
    |> Dictionary<'a, 'b>

let toJsonBytes object = 
    JsonSerializer.Serialize(object)
    |> Encoding.UTF8.GetBytes

let fromJsonBytes<'a> (bytes: byte array) =
    Encoding.UTF8.GetString(bytes)
    |> JsonSerializer.Deserialize<'a>


(*
let factory = ConnectionFactory(HostName = "localhost")

let connection = factory.CreateConnection()
let channel = connection.CreateModel()
*)

let startQueueDeclare name = {
    Name = name;
    Durable = false;
    AutoDelete = false;
    Exclisive = false;
    Args = Map.empty
}

let durable value = 
    { value with Durable = true } 

let autoDelete value = 
    { value with AutoDelete = true }

let exclusive value = 
    { value with Exclisive = true }

let withArgs args value = 
    { value with Args = args }


let executeQueueDeclare (channel: IModel) (opts: QueueDeclareOpts) = 
    channel.QueueDeclare(
        queue = opts.Name,
        durable = opts.Durable,
        exclusive = opts.Exclisive,
        autoDelete = opts.AutoDelete,
        arguments = mapToDict opts.Args
    )


let startPublish (body: byte array) = {
    Exchange = String.Empty;
    Routing = String.Empty
    Properties = null;
    Body = body;
}

let startPublishModel model = {
    Exchange = String.Empty;
    Routing = String.Empty
    Properties = null;
    Body = toJsonBytes model;
}

let withRouting routing publish =
    { publish with Routing = routing }

let withExchange exchange publish =
    { publish with Exchange = exchange }

let withProperties props publish = 
    { publish with Properties = props }

let executePublish (channel: IModel) (opts: PublishOpts) = 
    channel.BasicPublish(
        opts.Exchange,
        opts.Routing,
        opts.Properties,
        opts.Body
    )

let bindQueue queue exchange route (channel: IModel)  = 
    channel.QueueBind(queue, exchange, route)

let declareFanoutExchange name (channel: IModel) = 
    channel.ExchangeDeclare(name, ExchangeType.Fanout)

let declareDirectExchange name (channel: IModel) = 
    channel.ExchangeDeclare(name, ExchangeType.Direct)