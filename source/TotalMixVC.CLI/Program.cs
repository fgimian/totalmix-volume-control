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
            // Configure the volume increment.
            const float volumeIncrement = 0.01f;

            // Configure the OSC address for the master volume.
            const string volumeAddress = "/1/mastervolume";

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
                        if (message.Address != volumeAddress)
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
                    .Send(new OscMessage(volumeAddress, -1.0f))
                    .ConfigureAwait(false);

                while (currentVolume == -1.0f)
                {
                    // Wait until the current volume is updated by the listener.
                    await Task.Delay(25).ConfigureAwait(false);
                }

                while (true)
                {
                    ConsoleKeyInfo keyInfo = Console.ReadKey();
                    if (keyInfo.Key is not ConsoleKey.UpArrow and not ConsoleKey.DownArrow)
                    {
                        continue;
                    }

                    if (keyInfo.Key == ConsoleKey.UpArrow)
                    {
                        currentVolume += volumeIncrement;
                        Console.WriteLine($"Increasing volume to {currentVolume}");
                    }
                    else
                    {
                        currentVolume -= volumeIncrement;
                        Console.WriteLine($"Decreasing volume to {currentVolume}");
                    }

                    await sender
                        .Send(new OscMessage(volumeAddress, currentVolume))
                        .ConfigureAwait(false);
                }
            });

            Task.WaitAll(listenerTask, senderTask);
        }
    }
}
