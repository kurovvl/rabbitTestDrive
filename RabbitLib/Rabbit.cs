using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Net.Mime;
using System.Text;
using System.Threading.Channels;

namespace RabbitLib
{
    public class Rabbit
    {
        const string queueName = "hello";
        RabbitMQ.Client.ConnectionFactory factory;
        enum msgType { MyType, YourType };
        public Rabbit()
        {
            factory = new ConnectionFactory()
            {
                HostName = "localhost", // Если localhost то логин/пароль по-умолчанию (guest/guest) и их можно не указывать,
                                        // иначе нужно указать UserName и Password
                //UserName = "login",
                //Password = "password",
            };
        }
        public IModel DeclareChannel(IConnection connection)
        {
            var cnl = connection.CreateModel();

            cnl.QueueDeclare(
                             queue: queueName,  // Имя очереди. Если имя не указано, сервер сгенерирует случайное имя.
             
                             durable: true,     // Если true, очередь будет "постоянной",
                                                // то есть она будет переживать перезагрузку брокера.
                                                // Обратите внимание, что это не означает, что сообщения в очереди будут сохранены;
                                                // если вы хотите, чтобы сообщения также были постоянными, вам нужно будет опубликовать
                                                // их с установленным свойством deliveryMode равным 2 => Ниже в функции SetQueue это свойство Persistent .

                             // Важное замечание: 
                             // Подписка на очередь для Sender/Listener должны иметь одинаковый параметр durable, например,
                             // consumer подписался на очередь с durable = false, при попытке отправки сообщения
                             // в очередь с durable = true возникнет  исключение, и наоборот
                             // посмотреть/удалить/добавить очереди можно программно или на вкладке http://localhost:15672/#/queues (логин/пароль для localhost - guest/guest)
                             // Если не открывается - в командной строке рэббита (RabbitMQ Command Prompt в меню пуск - откроет cmd из папки сервера)
                             // ввести команду rabbitmq-plugins enable rabbitmq_management
                             // веб интерфейс очень богатый, внизу есть пункты меню со стрелочками, которые не очень то и заметны 

                             exclusive: false,  // Если true, очередь будет использоваться только одним соединением, и очередь будет удалена при закрытии этого соединения.
             
                             autoDelete: false, // Если true, очередь будет автоматически удалена, когда последний потребитель отменит подписку.
             
                             arguments: null    // Дополнительные аргументы, которые могут быть использованы для некоторых расширений RabbitMQ. В большинстве случаев это может быть null.
                             );
            return cnl;
        }
        public void GetQueue()
        {
            
            using (var connection = factory.CreateConnection())
            using (var channel = DeclareChannel(connection))
            {
                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var t = ea.BasicProperties.IsTypePresent() ? ea.BasicProperties.Type : "N/A";  // Получает тип данных сообщения
                    var ct = ea.BasicProperties.IsContentTypePresent()? ea.BasicProperties.ContentType : "N/A";// Получает формат данных сообщения
                    Console.WriteLine($" [x] Received {{0}} @ {DateTime.Now}, msgType: '{t}', contentType: '{ct}'", message);
                    
                    

                };
                channel.BasicConsume(queue: queueName,      // Имя очереди. Если имя не указано, сервер сгенерирует случайное имя.

                                     autoAck: true,         // Если true, RabbitMQ автоматически подтвердит каждое полученное сообщение.
                                                            // Это означает, что как только сообщение доставлено в ваше приложение,
                                                            // RabbitMQ сразу же пометит его как обработанное и удалит из очереди.
                                                            // Если false, вам нужно будет явно подтвердить обработку сообщения, вызвав метод BasicAck.
                                                            // Если ваше приложение упадет перед тем, как оно подтвердит обработку сообщения,
                                                            // RabbitMQ будет считать, что сообщение не было обработано, и попытается доставить его снова.

                                     consumer: consumer);   // Объект, который будет получать сообщения из очереди.
                                                            // Обычно это экземпляр класса, который реализует интерфейс IBasicConsumer.
                                                          

                Console.WriteLine(" Press [enter] to exit.");
                Console.ReadLine();
            }
        }

        public void SetQueue(string message)
        {

            using (var connection = factory.CreateConnection())
            using (var channel = DeclareChannel(connection))
            {
                var body = Encoding.UTF8.GetBytes(message);         // Преобразует строку в массив байт 

                var properties = channel.CreateBasicProperties();
                properties.Persistent = false; // Устанавливает  DeliveryMode = 1|2

                //properties.ContentType = "application/json" ;   // Это поле не обязательно для заполнения, но его использование может помочь получателям сообщений понять,
                                                                // как обрабатывать содержимое сообщения.
                                                                // Некоторые общепринятые значения ContentType включают:
                                                                //     "text/plain": для простого текстового содержимого.
                                                                //     "application/json": если тело сообщения представляет собой JSON.
                                                                //     "application/octet-stream": для бинарных данных.

                // Важное замечание:
                // Вы можете установить свои собственные типы сообщений и использовать их в качестве значения для ContentType.
                // Это может быть полезно, если у вас есть разные типы сообщений, которые требуют разной обработки при получении.
                // Однако, стоит отметить, что ContentType предназначен для указания формата данных сообщения,
                // а не для указания логического типа сообщения в вашем приложении.
                // Если вы хотите указать тип сообщения, возможно, будет лучше использовать другое свойство, например, Type.

                properties.Type = "MyType";
                properties.Type = $"{msgType.MyType}";

                channel.BasicPublish(exchange: "",                  // Имя обменника, через который будет отправлено сообщение.
                                                                    // Обменники принимают сообщения от производителей и направляют их
                                                                    // в очереди в соответствии с правилами маршрутизации.
                                                                    // Если имя обменника пустое, сообщение будет отправлено напрямую в очередь, указанную в routingKey.

                                     routingKey: queueName,         // Имя очереди или ключ маршрутизации, который будет использоваться для определения,
                                                                    //  в какую очередь должно быть отправлено сообщение.
                                                                    //  Если вы используете прямой обменник, это должно быть имя очереди.
                                                                    //  Если вы используете тематический или обменник заголовков, это будет шаблон,
                                                                    //  который будет сопоставлен с ключами маршрутизации сообщений.

                                     basicProperties: properties,   // nullable Свойства сообщения. Это может включать такие вещи, как deliveryMode,
                                                                    // который определяет, будет ли сообщение постоянным, и contentType,
                                                                    // который определяет тип содержимого сообщения.

                                     body: body);                   // Тело сообщения. Это должно быть массив байтов, который представляет собой содержимое сообщения.
                                                                    // В официальной документации RabbitMQ рекомендуется ограничивать размер сообщений примерно 200 КБ
                                                                    // для оптимальной производительности, хотя теоретически ограничения на размер сообщения нет.
                                                                    // RabbitMQ хранит сообщения в памяти, поэтому отправка большого количества больших сообщений
                                                                    // может привести к исчерпанию памяти и снижению производительности.

                Console.WriteLine($" [x] Sent {{0}} {DateTime.Now}:", message);
            }
        }
    }
}
