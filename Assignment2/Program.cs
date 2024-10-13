using System;
using System.Threading;

namespace Assignment2
{


    internal class Program
    {
        public delegate void priceCutEvent(Int32 pr); // Define a delegate
        int count = 0;
        public class ParkingStructure
        {

            private static Semaphore _pool;
            private static int padding = 0;

            static Random rng = new Random(); // To generate random numbers
            public static event priceCutEvent priceCut; // Link event to delegate
            private static Int32 parkingPrice = 10;
            private static Int32 count = 0;
            public static MultiCellBuffer buffer = new MultiCellBuffer();

            public class Project2
            {
                static void Main(string[] args)
                {
                    //create semaphore
                    _pool = new Semaphore(0, 3);

                    //create parking structure to change prices and notify agent
                    ParkingStructure pAgent = new ParkingStructure();
                    Thread parkAgent = new Thread(new ThreadStart(pAgent.PricingModel));
                    parkAgent.Start();
                    ParkingAgent parkingAgent = new ParkingAgent();


                    Thread[] parkingAgent2 = new Thread[5];

                    for (int i = 0; i < 5; i++) // N = 5 here
                    { // Start N agent threads
                        parkingAgent2[i] = new Thread(new ThreadStart(parkingAgent.placeOrder));
                        parkingAgent2[i].Name = (i + 1).ToString();
                        parkingAgent2[i].Start();
                    }
                    _pool.Release(3);


                    Console.ReadLine();
                }
            }


            public Int32 getPrice()
            {
                return parkingPrice;
            }
            public static void changePrice(Int32 price)
            {
                //check for price cut for event
                if (price < parkingPrice) 
                {
                    if (priceCut != null) // there is at least a subscriber
                        priceCut(price);

                    Console.WriteLine("================SALE================ $" + price + "\n");

                }
                //change parking price
                parkingPrice = price;
            }


            //calculate price and change price sometimes
            public void PricingModel()
            {
                for (Int32 i = 0; i < 20; i++)
                {
                    Thread.Sleep(500);
                    // Decide the price
                    Int32 p = rng.Next(10, 40);
                    Console.WriteLine("----------Price change #" + (i + 1) +
                        "----------: New Price is $" + p + "\n");
                    ParkingStructure.changePrice(p);
                }
            }

            //calculate charge
            public void OrderProcessing()
            {
                //get order from buffer cell
                OrderClass order = new OrderClass();
                buffer.getOneCell(ref order, ref count);



                //check if the card is within the accepted number range
                if (order.CardNo > 3000 && order.CardNo < 7000)
                {

                    //calculate the price and output to console
                    double price = ((order.UnitPrice * order.Quantity) * 1.10) + rng.Next(2, 8);
                    Console.WriteLine("Processed order from agent [" + order.SenderId + "]: $" + price
                         + " ---- cardNo: " + order.CardNo + " ---- amount: " + order.Quantity + 
                         " ---- price per ticket : $" + order.UnitPrice);
                }
                else
                {

                    //output card declined if number isnt in acceptable range
                    Console.WriteLine("Processed order from agent [" + order.SenderId + "]: Card declined");
                }

            }

            public class ParkingAgent
            {

                //create an order for the parking agent
                public void placeOrder()
                {
                    //declare object to send order to buffer 
                    OrderClass order = new OrderClass();
                    ParkingStructure agent = new ParkingStructure();


                    for (Int32 i = 0; i < 10; i++)
                    {
                        
                        Thread.Sleep(1000);
                        Int32 p = agent.getPrice();


                        //generate information for order
                        order.SenderId = Thread.CurrentThread.Name;
                        order.CardNo = rng.Next(1000, 9999);
                        order.Quantity = rng.Next(1, 3);
                        order.UnitPrice = p;

                        //shows that an order was made by agent
                        Console.WriteLine("Agent [" + order.SenderId  + "] made an order");

                        //sends order to buffer
                        buffer.setOneCell(ref order, ref count);

                        //show that order was sent
                        Console.WriteLine("Agent [" + order.SenderId + "] sent order to buffer");

                        agent.OrderProcessing();
                    }
                }
            }

            public class OrderClass
            {
                //variables for order information
                string senderId = "";
                int cardNo = 0;
                int quantity = 0;
                double unitPrice = 0;

                //getter and setter for variable
                public string SenderId { get => senderId; set => senderId = value; }
                public int CardNo { get => cardNo; set => cardNo = value; }
                public double UnitPrice { get => unitPrice; set => unitPrice = value; }
                public int Quantity { get => quantity; set => quantity = value; }
            }

            //Recieve order from parkingAgent
            public class MultiCellBuffer
            {
                //reference to order class
                OrderClass[] buffer = new OrderClass[3];

                internal OrderClass[] Buffer { get => buffer; set => buffer = value; }

                //function sets the buffer cell order info to provided order
                public void setOneCell(ref OrderClass order, ref int cell)
                {
                    //request resource
                    _pool.WaitOne();
                    padding = padding + 100;
                    Thread.Sleep(100 );

                    //lock
                    Monitor.Enter(buffer);
                    try
                    {
                        //add order to buffer and increase cell count
                        buffer[cell] = order;
                        cell++;
                    }
                    finally
                    {

                        Monitor.Exit(buffer);
                    }

                    //release semaphore
                    _pool.Release();
                }
                //function sets the buffer cell order info to provided order
                public void getOneCell(ref OrderClass order, ref int cell)
                {
                    //request resource
                    _pool.WaitOne();

                    //lock
                    Monitor.Enter(buffer);
                    try
                    {
                        //get order from the buffer and decrease cell count
                        order = buffer[cell - 1];
                        cell--;
                    }
                    finally
                    {

                        Monitor.Exit(buffer);
                    }
                    //release semaphore
                    _pool.Release();
                }
            }

        }
    }
}
