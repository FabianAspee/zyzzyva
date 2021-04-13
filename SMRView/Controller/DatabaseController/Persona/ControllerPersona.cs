using Grpc.Core;
using SMRView.Controller.ControllerContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZyzzyvagRPC.Services;

namespace SMRView.Controller
{

    /// <include file="Docs/Controller/DatabaseController/Persona/ControllerPersona.xml" path='docs/members[@name="controllerpersona"]/ControllerPersonaC/*'/>
    public class ControllerPersona : BaseController, IAsyncDisposable, IControllerPersona
    {

        private readonly DataBase.DataBaseClient _clientPersona;
        private readonly AsyncDuplexStreamingCall<ReadRequest, ReadResponseS> _duplexStreamPersonaRead;
        private readonly AsyncDuplexStreamingCall<WriteRequest, WriteResponse> _duplexStreamPersonaWrite;

        /// <include file="Docs/Controller/DatabaseController/Persona/ControllerPersona.xml" path='docs/members[@name="controllerpersona"]/ReadResponse/*'/>
        public EventHandler<Dictionary<int, PersonagRPC>> ReadResponse { get; set; }

        /// <include file="Docs/Controller/DatabaseController/Persona/ControllerPersona.xml" path='docs/members[@name="controllerpersona"]/ReadAllResponses/*'/>
        public EventHandler<Dictionary<int, List<PersonagRPC>>> ReadAllResponses { get; set; }

        /// <include file="Docs/Controller/DatabaseController/Persona/ControllerPersona.xml" path='docs/members[@name="controllerpersona"]/GeneralResponses/*'/>
        public EventHandler<(string,bool)> GeneralResponses { get; set; }

        /// <include file="Docs/Controller/DatabaseController/Persona/ControllerPersona.xml" path='docs/members[@name="controllerpersona"]/ControllerPersona/*'/>
        public ControllerPersona()
        {
            _clientPersona = new DataBase.DataBaseClient(channel);
            _duplexStreamPersonaRead = _clientPersona.SubscribeRead();
            _duplexStreamPersonaWrite = _clientPersona.SubscribeWrite();
            _ = HandleResponsesReadAsync();
            _ = HandleResponsesWriteAsync();
        }
        ///<inheritdoc/>
        public async Task Read(int id)
        {
            var x = new ReadRequest { Msg = new Read { Id = id } };
            await _duplexStreamPersonaRead.RequestStream.WriteAsync(x);
        }
        ///<inheritdoc/>
        public async Task ReadAll()
        {

            var x = new ReadRequest { Msg2 = new ReadAll { } };
            await _duplexStreamPersonaRead.RequestStream.WriteAsync(x);
        }
        ///<inheritdoc/>
        public async Task Insert(PersonagRPC persona)
        {

            var x = new WriteRequest { Msg = new Insert { Persona = persona } };
            await _duplexStreamPersonaWrite.RequestStream.WriteAsync(x);
        }
        ///<inheritdoc/>
        public async Task Update(PersonagRPC persona)
        {
            var x = new WriteRequest { Msg2 = new Update { Persona = persona } };
            await _duplexStreamPersonaWrite.RequestStream.WriteAsync(x);
        }
        ///<inheritdoc/>
        public async Task Delete(int id)
        {
            var x = new WriteRequest { Msg3 = new Delete { Id = id } };
            await _duplexStreamPersonaWrite.RequestStream.WriteAsync(x);
        }
        /// <inheritdoc/>
        public async Task<SetByzantineResponse> Byzantine(int id)
        {
            var x = new SetByzantineRequest { Id = id };

            var result = await _clientPersona.SetByzantineAsync(x);
            return result;
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            try
            {
                await _duplexStreamPersonaWrite.RequestStream.CompleteAsync();
                await _duplexStreamPersonaRead.RequestStream.CompleteAsync();
            }
            finally
            {
                _duplexStreamPersonaWrite.Dispose();
                _duplexStreamPersonaRead.Dispose();
            }
        }

        private async Task HandleResponsesReadAsync()
        {

            await foreach (var update in _duplexStreamPersonaRead.ResponseStream.ReadAllAsync())
            {
                GeneralResponses.Invoke(this, ResponseRead(update)); 
            }
        }
        private async Task HandleResponsesWriteAsync()
        {
            await foreach (var update in _duplexStreamPersonaWrite.ResponseStream.ReadAllAsync())
            {
                GeneralResponses.Invoke(this, ResponseReadAll(update.ReadAllResponse)); 
            }
        }
        private (string, bool) ResponseRead(ReadResponseWrapper response)
        {
            ReadResponse.Invoke(this, response.ResponseList.ToDictionary(x => x.Id, x => x.Response.Persona));
            return ($"Persona Nome {response.Response.Persona.Nome} Cognome {response.Response.Persona.Cognome} " +
            $"Eta {response.Response.Persona.Eta} Ha Machina? {response.Response.Persona.HaMacchina}", response.ResponseList.Count>0);
        }
        private (string,bool) ResponseReadAll(ReadAllResponseWrapper response)
        {
            ReadAllResponses.Invoke(this, response.ResponseList.ToDictionary(x => x.Id, x => x.Response.Persona.ToList()));
            return (string.Join($"\n", response.Response.Persona), response.Response.Persona.Count>0);
        }
        private (string, bool) ResponseRead(ReadResponseS response) => response.ActionCase switch
        {
            ReadResponseS.ActionOneofCase.Msg => ResponseRead(response.Msg),
            ReadResponseS.ActionOneofCase.Msg2 => ResponseReadAll(response.Msg2),
            _ => throw new NotImplementedException()
        };
    }
}
