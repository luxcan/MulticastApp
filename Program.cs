using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Net.NetworkInformation;

string multicastGroup;
int multicastPort;
int mode;
bool quit = false;

// List all available relevant IP addresses
List<IPAddress> localIPs = new List<IPAddress>();
int ipIndex = 1;

Console.WriteLine("Select a local IP address for sending/receiving:");
foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces()) {
    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses) {
        if (ip.Address.AddressFamily == AddressFamily.InterNetwork &&
            !IPAddress.IsLoopback(ip.Address) &&
            !ip.Address.ToString().StartsWith("169.254")) {
            localIPs.Add(ip.Address);
            Console.WriteLine($"{ipIndex++}. {ip.Address}");
        }
    }
}

IPAddress localIP = null!;
while (true) {
    Console.Write("Enter the number of the IP address: ");
    if (int.TryParse(Console.ReadLine(), out int ipChoice) && ipChoice > 0 && ipChoice <= localIPs.Count) {
        localIP = localIPs[ipChoice - 1];
        break;
    }
    Console.WriteLine("Invalid choice. Please select a valid number.");
}

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
    StartSender(localIP, multicastGroup, multicastPort, ref quit);
} else {
    StartReceiver(localIP, multicastGroup, multicastPort, ref quit);
}

static void StartSender(IPAddress localIP, string multicastGroup, int multicastPort, ref bool quit) {
    using (var udpClient = new UdpClient(new IPEndPoint(localIP, 0))) {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Parse(multicastGroup), multicastPort);
        Console.Write("Enter the message to send: ");
        string message = Console.ReadLine()!;

        byte[] data = Encoding.UTF8.GetBytes(message);

        Console.WriteLine($"Sending multicast packets to {multicastGroup}:{multicastPort}... Press 'Esc' to quit.");

        while (!quit) {
            udpClient.Send(data, data.Length, remoteEndPoint);
            Console.WriteLine($"{DateTime.Now} = Sent");
            Thread.Sleep(1000);
        }
    }
}

static void StartReceiver(IPAddress localIP, string multicastGroup, int multicastPort, ref bool quit) {
    using (UdpClient udpClient = new UdpClient(new IPEndPoint(localIP, multicastPort))) {
        udpClient.ExclusiveAddressUse = false;

        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        udpClient.JoinMulticastGroup(IPAddress.Parse(multicastGroup));

        Console.WriteLine($"Listening for multicast packets on {multicastGroup}:{multicastPort}... Press 'Esc' to quit.");

        while (!quit) {
            if (udpClient.Available > 0) {
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, multicastPort);
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                try {
                    string message = Encoding.UTF8.GetString(data);
                    Console.WriteLine($"{DateTime.Now} = Received from \"{remoteEndPoint.Address}:{remoteEndPoint.Port}\": {message}");
                } catch (DecoderFallbackException) {
                    Console.WriteLine($"{DateTime.Now} = Received (Not in UTF8) from \"{remoteEndPoint.Address}:{remoteEndPoint.Port}\": Bytes Length ({data.Length})");
                } catch (Exception) {
                    Console.WriteLine($"{DateTime.Now} = Received from \"{remoteEndPoint.Address}:{remoteEndPoint.Port}\": Encounter exception while trying to convert data into UTF8");
                }
            }
        }
    }
}