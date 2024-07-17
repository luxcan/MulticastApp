using System.Net;
using System.Net.Sockets;
using System.Text;

string multicastGroup;
int multicastPort;
int mode;
bool quit = false;

while (true) {
    Console.Write("Enter the multicast IP address: ");
    multicastGroup = Console.ReadLine()!;

    if (IPAddress.TryParse(multicastGroup, out _)) {
        break;
    }

    Console.WriteLine("Invalid IP address.");
}

while (true) {
    Console.Write("Enter the multicast port: ");
    if (int.TryParse(Console.ReadLine(), out multicastPort) && multicastPort > 0 && multicastPort <= 65535) {
        break;
    }

    Console.WriteLine("Invalid port number. Please enter a number between 1 and 65535.");
}

while (true) {
    Console.Write("Select mode (1 - Sender/2 - Receiver): ");
    if (int.TryParse(Console.ReadLine(), out mode) && (mode == 1 || mode == 2)) {
        break;
    }

    Console.WriteLine("Invalid choice. Please enter 1 for Sender or 2 for Receiver.");
}

// Listen for "Esc" key press
Thread keyListener = new Thread(() => {
    while (!quit) {
        if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape) {
            quit = true;
        }
    }
});
keyListener.Start();

if (mode == 1) {
    StartSender(multicastGroup, multicastPort, ref quit);
} else {
    StartReceiver(multicastGroup, multicastPort, ref quit);
}

static void StartSender(string multicastGroup, int multicastPort, ref bool quit) {
    using (var udpClient = new UdpClient()) {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(multicastGroup), multicastPort);
        Console.Write("Enter the message to send: ");
        string message = Console.ReadLine()!;

        byte[] data = Encoding.UTF8.GetBytes(message);

        Console.WriteLine($"Sending multicast packets to {multicastGroup}:{multicastPort}... Press 'Esc' to quit.");

        while (!quit) {
            udpClient.Send(data, data.Length, remoteEndPoint);
            Console.WriteLine($"{DateTime.Now} = Send");
            Thread.Sleep(1000);
        }
    }
}

static void StartReceiver(string multicastGroup, int multicastPort, ref bool quit) {
    using (UdpClient udpClient = new UdpClient()) {
        udpClient.ExclusiveAddressUse = false;

        IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, multicastPort);
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(localEndPoint);

        udpClient.JoinMulticastGroup(IPAddress.Parse(multicastGroup));

        Console.WriteLine($"Listening for multicast packets on {multicastGroup}:{multicastPort}... Press 'Esc' to quit.");

        while (!quit) {
            if (udpClient.Available > 0) {
                byte[] data = udpClient.Receive(ref localEndPoint);
                try {
                    string message = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"{DateTime.Now} = Received: {message}");
                } catch (DecoderFallbackException) {
                    Console.WriteLine($"{DateTime.Now} = Received (Not in UTF8): Bytes Length ({data.Length})");
                } catch (Exception) {
                    Console.WriteLine($"{DateTime.Now} = Received but encounter exception while trying to convert data into UTF8");
                }
            }
        }
    }
}