using Warehouse.Backend.Core;
using Warehouse.Backend.Features.Okx.Messages.Socket;

namespace Warehouse.Backend.Features.Okx.Handlers;

public interface IOkxMessageHandler : IMessageHandler<OkxSocketResponse>;
