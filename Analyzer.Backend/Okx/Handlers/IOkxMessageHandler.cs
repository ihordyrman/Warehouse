using Analyzer.Backend.Core;
using Analyzer.Backend.Okx.Messages;
using Analyzer.Backend.Okx.Messages.Socket;

namespace Analyzer.Backend.Okx.Handlers;

public interface IOkxMessageHandler : IMessageHandler<OkxSocketResponse>;
