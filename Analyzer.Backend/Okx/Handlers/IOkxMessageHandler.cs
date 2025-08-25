using Analyzer.Backend.Core;
using Analyzer.Backend.Okx.Messages;

namespace Analyzer.Backend.Okx.Handlers;

public interface IOkxMessageHandler : IMessageHandler<OkxSocketResponse>;
