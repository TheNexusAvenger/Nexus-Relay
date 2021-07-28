# Nexus Relay
Nexus Relay forwards TCP and UDP traffic data
to external hosts. It can be thought of as a
reverse proxy like nginx, but redirects to other
systems rather than locally. This can be used to
run services on multiple hosts while allowing
traffic to be accepted from a single host.

## Compared To ngrok
Ngrok is a cloud-hosted, more robust alternative
that doesn't properly handle TCP and UDP traffic
how some hosts expect it. Some applications send
TCP and UDP on the same port, and Nexus Relay
ensures that the TCP and UDP traffic are sent
together. Nexus Relay is also expected to be
self-hosted on a smaller scale.

# Running
Both the server and client need to be run for
traffic to be forwarded. A couple parameters
are required, including a port to communicate
with clients and a secret for authenticating
clients.

```bash
./NexusRelayServer --port 9000 --secret my-secret
```

The client requires more arguments since it
needs to know the host server and where to
send traffic to.

```bash
./NexusRelayClient.exe --remote-host 127.0.0.1 --remote-port 9000 --port 8000 --redirect-host thenexusavenger.io --redirect-port 443 --secret my-secret
```

With both commands, the following happen:
- The server opens a communication stream on port 9000.
  - The secret for clients to use is `my-secret`.
- The client sets up traffic forwarding on the server.
  - The port used on the host is 8000.
  - Traffic is forwarded to thenexusavenger.io:443

## Public Hosting
Public hosting of Nexus Relay is not provided
as other publicly-hosted services, like Serveo,
have had to shut down due to malcious clients.