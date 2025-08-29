using Warehouse.Backend.Core;
using Warehouse.Backend.Markets.Okx.Messages.Socket;

namespace Warehouse.Backend.Markets.Okx.Handlers;

public interface IOkxMessageHandler : IMessageHandler<OkxSocketResponse>;
