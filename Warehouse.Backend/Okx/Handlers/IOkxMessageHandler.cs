using Warehouse.Backend.Okx.Messages;
using Warehouse.Backend.Core;
using Warehouse.Backend.Okx.Messages.Socket;

namespace Warehouse.Backend.Okx.Handlers;

public interface IOkxMessageHandler : IMessageHandler<OkxSocketResponse>;
