using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using OscCore;
using TotalMixVC.Communicator;

namespace TotalMixVC.CLI
{
    internal static class Program
    {
        internal static void Main()
        {
            // Initialise the current volume to an invalid value so we can track when it's ready.
            float currentVolume = -1.0f;

            var listenerTask = Task.Run(async () =>
            {
                var listener = new Listener(new IPEndPoint(IPAddress.Loopback, 9001));

                while (true)
                {
                    OscPacket packet = await listener.Receive().ConfigureAwait(false);
                    if (packet is not OscBundle)
                    {
                        continue;
                    }

                    OscBundle bundle = packet as OscBundle;
                    IEnumerator<OscMessage> messageEnumerator = bundle.Messages();

                    while (messageEnumerator.MoveNext())
                    {
                        OscMessage message = messageEnumerator.Current;

                        // Only process master volume messages.
                        if (message.Address != "/1/mastervolume")
                        {
                            continue;
                        }

                        // Master volume messages should only contain one value.
                        if (message.Count != 1)
                        {
                            continue;
                        }

                        // Obtain the value as a float.
                        try
                        {
                            currentVolume = (float)message[0];
                        }
                        catch (InvalidCastException)
                        {
                            continue;
                        }

                        Console.WriteLine($"Volume updated to {currentVolume}");
                    }
                }
            });

            var senderTask = Task.Run(async () =>
            {
                var sender = new Sender(new IPEndPoint(IPAddress.Loopback, 7001));

                // Send an initial invalid value so that TotalMix can send us the current volume.
                await sender
                    .Send(new OscMessage("/1/mastervolume", -1.0f))
                    .ConfigureAwait(false);

                while (currentVolume == -1.0f)
                {
                    // Wait until the current volume is updated by the listener.
                }

                while (true)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey();
                    if (keyInfo.Key == ConsoleKey.UpArrow)
                    {
                        currentVolume += 0.01f;
                        Console.WriteLine($"Increasing volume to {currentVolume}");

                        await sender
                            .Send(new OscMessage("/1/mastervolume", currentVolume))
                            .ConfigureAwait(false);
                    }
                    else if (keyInfo.Key == ConsoleKey.DownArrow)
                    {
                        currentVolume -= 0.01f;
                        Console.WriteLine($"Decreasing volume to {currentVolume}");

                        await sender
                            .Send(new OscMessage("/1/mastervolume", currentVolume))
                            .ConfigureAwait(false);
                    }
                }
            });

            Task.WaitAll(listenerTask, senderTask);
        }
    }
}
