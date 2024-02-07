// See https://aka.ms/new-console-template for more information
using RabbitLib;
var r = new Rabbit();
var msg = "";
while ((msg = Console.ReadLine()) != "quit")
{
    r.SetQueue(msg);
}

