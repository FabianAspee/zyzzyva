using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Zyzzyva.Database.Tables;
using ZyzzyvagRPC.Subscriber.SubscriberContract;
using ZyzzyvagRPC.Subscriber.SubscriberFactory;

namespace ZyzzyvagRPC.Services
{
    /// <include file="../Docs/Services/DataBaseService.xml" path='docs/members[@name="databaseservice"]/DataBaseService/*'/>
    public class DataBaseService : DataBase.DataBaseBase
    {
        private readonly ISubscriberFactory _factoryMethod;
        private readonly ILogger<DataBaseService> _logger;
        /// <include file="../Docs/Services/DataBaseService.xml" path='docs/members[@name="databaseservice"]/DataBaseServiceC/*'/>
        public DataBaseService(ISubscriberFactory factoryMethod, ILogger<DataBaseService> logger)
        {
            _logger = logger;
            _factoryMethod = factoryMethod;
        }
        /// <include file="../Docs/Services/DataBaseService.xml" path='docs/members[@name="databaseservice"]/SubscribeWrite/*'/>
        public override async Task SubscribeWrite(IAsyncStreamReader<WriteRequest> request, IServerStreamWriter<WriteResponse> response, ServerCallContext context)
        {

            _logger.LogInformation("Subscription started.");
            using var subscriberWriter = _factoryMethod.GetPersonSubscriber();

            subscriberWriter.InsertEvent += async (sender, args) =>
               await WriteOperationAsyncResponse(response, args.PersonaResult, args.ReplicasResult,args.FinalStatus);

            subscriberWriter.UpdateEvent += async (sender, args) =>
            await WriteOperationAsyncResponse(response, args.PersonaResult, args.ReplicasResult, args.FinalStatus);

            subscriberWriter.DeleteEvent += async (sender, args) =>
            await WriteOperationAsyncResponse(response, args.PersonaResult, args.ReplicasResult, args.FinalStatus);

            subscriberWriter.CreateActor();
            try
            {
                await HandleActionsWrite(request, subscriberWriter);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.ToString());
            }
            _logger.LogInformation("Subscription finished.");
        }

        private static Task ByzantineAsyncResponse(TaskCompletionSource<bool> taskResponse, bool response)
        {
            taskResponse.SetResult(response);
            return Task.FromResult(taskResponse);
        }

        /// <include file="../Docs/Services/DataBaseService.xml" path='docs/members[@name="databaseservice"]/SetByzantine/*'/>
        public override async Task<SetByzantineResponse> SetByzantine(SetByzantineRequest request, ServerCallContext context)
        {
            using var setByzantine = _factoryMethod.GetPersonSubscriber();
            TaskCompletionSource<bool> task = new();
            setByzantine.SetByzantineEvent += async (sender, args) =>
                await ByzantineAsyncResponse(task, args.SetByzantineResponse);
            setByzantine.CreateActor();
            setByzantine.SetByzantine(request.Id);
            var result = await task.Task;
            return new SetByzantineResponse
            {
                Byzantine = result
            };
        }

        /// <include file="../Docs/Services/DataBaseService.xml" path='docs/members[@name="databaseservice"]/SubscribeRead/*'/>
        public override async Task SubscribeRead(IAsyncStreamReader<ReadRequest> request, IServerStreamWriter<ReadResponseS> response, ServerCallContext context)
        {

            _logger.LogInformation("Subscription started.");

            using var subscriberReader = _factoryMethod.GetPersonSubscriber();

            subscriberReader.ReadEvent += async (sender, args) =>
                await ReadOperationAsyncResponse(response, args.PersonaResult, args.ReplicasResult);

            subscriberReader.ReadAllEvent += async (sender, args) =>
            await ReadAllOperationAsyncResponse(response, args.PersonaResult, args.ReplicasResult, args.FinalStatus);

            subscriberReader.CreateActor();
            try
            {

                await HandleActionsRead(request, subscriberReader);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e.ToString());
            }

            _logger.LogInformation("Subscription finished.");
        }

        private async Task WriteOperationAsyncResponse(IServerStreamWriter<WriteResponse> stream, ImmutableList<Persona> allPerson, Dictionary<int, ImmutableList<Persona>> replicaResponses, bool finalStatus)
        { 
            try
            {  
                var response = new WriteResponse
                {
                    ReadAllResponse = new ReadAllResponseWrapper
                    {
                        Response = new ReadAllResponse(),
                        Status = finalStatus
                    }
                };
                response.ReadAllResponse.Response.Persona.AddRange(CreatePersonagRPC(allPerson));
                response.ReadAllResponse.ResponseList.AddRange(CreateReplicaReadAllResponse(replicaResponses));

                await stream.WriteAsync(response);
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to write message: {e.Message}");
            }
        }
        private static List<PersonagRPC> CreatePersonagRPC(ImmutableList<Persona> personas) => personas.Select(persona => new PersonagRPC
        {
            Id = persona.id,
            Cognome = persona.cognome,
            Eta = persona.eta,
            HaMacchina = persona.haMacchina,
            Nome = persona.nome
        }).ToList();
        private static List<ReplicaReadResponse> CreateReplicaReadResponse(Dictionary<int, Persona> personas) => personas.ToList().Select(persona => new ReplicaReadResponse
        {
            Response = new ReadResponse
            {
                Persona = new PersonagRPC
                {
                    Id = persona.Value.id,
                    Cognome = persona.Value.cognome,
                    Eta = persona.Value.eta,
                    HaMacchina = persona.Value.haMacchina,
                    Nome = persona.Value.nome
                }
            },
            Id = persona.Key
        }).ToList();
        private async Task ReadOperationAsyncResponse(IServerStreamWriter<ReadResponseS> stream, Persona persona, Dictionary<int, Persona> replicaResponses)
        {
            try
            {
                var response = new ReadResponseS
                {
                    Msg = new ReadResponseWrapper
                    {
                        Response = new ReadResponse
                        {
                            Persona = new PersonagRPC
                            {
                                Id = persona.id,
                                Cognome = persona.cognome,
                                Eta = persona.eta,
                                HaMacchina = persona.haMacchina,
                                Nome = persona.nome
                            }
                        },
                    }
                };
                response.Msg.ResponseList.AddRange(CreateReplicaReadResponse(replicaResponses));
                await stream.WriteAsync(response);
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to write message: {e.Message}");
            }
        }
        private static ReadAllResponse GetReadAllResponse(ImmutableList<Persona> allPerson)
        {
            var result = new ReadAllResponse();
            result.Persona.AddRange(CreatePersonagRPC(allPerson));
            return result;
        }
        private static List<ReplicaReadAllResponse> CreateReplicaReadAllResponse(Dictionary<int, ImmutableList<Persona>> personas) =>
            personas.ToList().Select(persona => new ReplicaReadAllResponse
            {
                Response = GetReadAllResponse(persona.Value),
                Id = persona.Key
            }).ToList();

        private async Task ReadAllOperationAsyncResponse(IServerStreamWriter<ReadResponseS> stream, ImmutableList<Persona> allPerson, Dictionary<int, ImmutableList<Persona>> replicaResponses, bool finalStatus)
        {
            try
            {
                var response = new ReadResponseS
                {
                    Msg2 = new ReadAllResponseWrapper
                    {
                        Response = new ReadAllResponse(),
                        Status = finalStatus
                    }
                };
                response.Msg2.Response.Persona.AddRange(CreatePersonagRPC(allPerson));
                response.Msg2.ResponseList.AddRange(CreateReplicaReadAllResponse(replicaResponses));
                await stream.WriteAsync(response);
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to write message: {e.Message}");
            }
        }
        private async Task HandleActionsWrite(IAsyncStreamReader<WriteRequest> requestStream, IPersonSubscriber subscriber)
        {
            await foreach (var action in requestStream.ReadAllAsync())
            {
                switch (action.ActionCase)
                {
                    case WriteRequest.ActionOneofCase.None:
                        _logger.LogWarning("No Action specified.");
                        break;
                    case WriteRequest.ActionOneofCase.Msg:
                        subscriber.Insert(ConvertgRPCToPerson(action.Msg.Persona));
                        break;
                    case WriteRequest.ActionOneofCase.Msg2:
                        subscriber.Update(ConvertgRPCToPerson(action.Msg2.Persona));
                        break;
                    case WriteRequest.ActionOneofCase.Msg3:
                        subscriber.Delete(action.Msg3.Id);
                        break;
                    default:
                        _logger.LogWarning($"Unknown Action '{action.ActionCase}'.");
                        break;
                }
            }
        }

        private async Task HandleActionsRead(IAsyncStreamReader<ReadRequest> requestStream, IPersonSubscriber subscriber)
        {
            await foreach (var action in requestStream.ReadAllAsync())
            {
                switch (action.ActionCase)
                {
                    case ReadRequest.ActionOneofCase.None:
                        _logger.LogWarning("No Action specified.");
                        break;
                    case ReadRequest.ActionOneofCase.Msg:
                        subscriber.Read(action.Msg.Id);
                        break;
                    case ReadRequest.ActionOneofCase.Msg2:
                        subscriber.ReadAll();
                        break;
                    default:
                        _logger.LogWarning($"Unknown Action '{action.ActionCase}'.");
                        break;
                }
            }
        }
        private static Persona ConvertgRPCToPerson(PersonagRPC personagRPC) => new Persona(
            personagRPC.Id == 0 ? 0 : personagRPC.Id,
            personagRPC.Nome,
            personagRPC.Cognome,
            personagRPC.Eta,
            personagRPC.HaMacchina);
    }
}
