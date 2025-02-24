# UDPService

UDPService is a C# library that facilitates communication over the User Datagram Protocol (UDP). It enables users to connect to devices via UDP, send and receive data, and handle connection events seamlessly.

## Features

- **Connection Management:** Connect to a specified IP address and port, and gracefully handle disconnections.
- **Data Transmission:** Send and receive data packets over UDP.
- **Event Handling:** Subscribe to events for connection established, disconnection, data received, and data sent.
- **Configurable Settings:** Adjust timeout durations and packet receive rates to suit application requirements.

## Installation

To include UDPService in your project, add the `UDPService` class to your solution. Ensure you have the necessary `using` directives:

```csharp
using System;
using UDPService;

 
