using System.Collections.Immutable;
using Zyzzyva.Akka.Replica.Messages.Response;
using Zyzzyva.Database.Tables;

namespace ZyzzyvaTest
{
    public static class TestResponses
    {
        private static readonly string YO_NON_ESISTO = "YO NON ESISTO";

        public readonly static Persona personaInsertId0 = new Persona(0, "juan", "el bandolero", 15, false);
        public readonly static Persona persona = new Persona(1, "juan", "el bandolero", 15, false);
        public readonly static Persona personaUpdateId0 = new Persona(0, "juan", "el bandolero", 15, false);
        public readonly static ReadPersonaResponse readPersona = new ReadPersonaResponse(new Persona(id: 0, nome: "Tu", cognome: "El pelon", eta: 12, haMacchina: true));
        public readonly static ReadPersonaResponse readPersonaNotExistId1 = new ReadPersonaResponse(new Persona(id: 1, nome: YO_NON_ESISTO, cognome: YO_NON_ESISTO, eta: 1, haMacchina: false));
        public readonly static ReadAllPersonaResponse readAllPersona = new ReadAllPersonaResponse(ImmutableList.Create(new Persona(id: 0, nome: "Tu", cognome: "El pelon", eta: 12, haMacchina: true)), true);
        public readonly static InsertPersonaResponse insertPersona = new InsertPersonaResponse(ImmutableList.Create(new Persona(id: 0, nome: "Tu", cognome: "El pelon", eta: 12, haMacchina: true),persona), true);
        public readonly static UpdatePersonaResponse updatePersona = new UpdatePersonaResponse(ImmutableList.Create(personaUpdateId0), true);
        public readonly static DeletePersonaResponse deletePersonaNotExist = new DeletePersonaResponse(ImmutableList.Create(new Persona(id: 0, nome: "Tu", cognome: "El pelon", eta: 12, haMacchina: true)), true);
        public readonly static DeletePersonaResponse deletePersona = new DeletePersonaResponse(ImmutableList<Persona>.Empty,true);
    }
}
