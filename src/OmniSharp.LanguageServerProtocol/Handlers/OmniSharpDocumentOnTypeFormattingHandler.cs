﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.JsonRpc;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Models.Format;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace OmniSharp.LanguageServerProtocol.Handlers
{
    internal sealed class OmniSharpDocumentOnTypeFormattingHandler : DocumentOnTypeFormattingHandlerBase
    {
        public static IEnumerable<IJsonRpcHandler> Enumerate(RequestHandlers handlers)
        {
            foreach (var (selector, handler) in handlers
                .OfType<Mef.IRequestHandler<FormatAfterKeystrokeRequest, FormatRangeResponse>>())
                if (handler != null)
                    yield return new OmniSharpDocumentOnTypeFormattingHandler(handler, selector);
        }

        private readonly Mef.IRequestHandler<FormatAfterKeystrokeRequest, FormatRangeResponse> _formatAfterKeystrokeHandler;
        private readonly DocumentSelector _documentSelector;

        public OmniSharpDocumentOnTypeFormattingHandler(Mef.IRequestHandler<FormatAfterKeystrokeRequest, FormatRangeResponse> formatAfterKeystrokeHandler, DocumentSelector documentSelector)
        {
            _formatAfterKeystrokeHandler = formatAfterKeystrokeHandler;
            _documentSelector = documentSelector;
        }

        public override async Task<TextEditContainer> Handle(DocumentOnTypeFormattingParams request, CancellationToken cancellationToken)
        {
            // TODO: request.options
            var omnisharpRequest = new FormatAfterKeystrokeRequest()
            {
                Character = request.Character,
                Line = Convert.ToInt32(request.Position.Line),
                Column = Convert.ToInt32(request.Position.Character),
                FileName = Helpers.FromUri(request.TextDocument.Uri),
            };

            var omnisharpResponse = await _formatAfterKeystrokeHandler.Handle(omnisharpRequest);
            return omnisharpResponse.Changes.Select(change => new TextEdit()
            {
                NewText = change.NewText,
                Range = new Range(new Position(change.StartLine, change.StartColumn), new Position(change.EndLine, change.EndColumn))
            }).ToArray();
        }

        protected override DocumentOnTypeFormattingRegistrationOptions CreateRegistrationOptions(DocumentOnTypeFormattingCapability capability, ClientCapabilities clientCapabilities)
        {
            return new DocumentOnTypeFormattingRegistrationOptions()
            {
                DocumentSelector = _documentSelector,
                FirstTriggerCharacter = ";",
                // TODO: What should these be?
                MoreTriggerCharacter = new[] {"}", ")"}
            };
        }
    }
}
