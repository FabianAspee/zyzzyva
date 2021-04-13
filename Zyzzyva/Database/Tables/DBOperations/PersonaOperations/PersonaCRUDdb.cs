using Hocon;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Zyzzyva.Database.Tables;

namespace Zyzzyva.Database
{
    /// <inheritdoc/>
    public class PersonaCRUDdb : IPersonaCRUD
    {
        private static readonly string YO_NON_ESISTO = "YO NON ESISTO";
        private static readonly string PATH2 = "config/dbconfig.hocon";
        private static readonly string PATH = "Database/Settings/dbconfig.hocon";
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/PersonaCRUDdb.xml" path='docs/members[@name="persona"]/PersonaCRUD/*'/>
        private readonly Dictionary<int, Persona> Persona = new Dictionary<int, Persona>();
        private Action lastAction;
        private int globalId = 0; 
        private readonly Config congi = HoconConfigurationFactory.FromFile(File.Exists(PATH2)?PATH2:PATH);
        private readonly string  dbpath = File.Exists(PATH2)?"dbpath.path2":"dbpath.path";
        /// <inheritdoc/>
        public ImmutableList<Persona> ReadAllPersone()
        {
            lastAction = default;
            return Persona.Values.ToImmutableList();
        }

        /// <inheritdoc/>
        public Persona ReadPersona(int id)
        {
            lastAction = default;
            return FindPersona(id);
        }

        /// <inheritdoc/>
        public (ImmutableList<Persona>,bool) InsertPersona(Persona persona)
        {
            var finalId = persona.id != 0 ? 
                (!Persona.ContainsKey(persona.id) ? 
                persona.id : 
                (!Persona.ContainsKey(globalId) ? 
                globalId++ : 
                Persona.Max(x => x.Key) + 1)): 
                Persona.Max(x => x.Key) + 1;
            Persona personaFInal = new(finalId, persona.nome, persona.cognome, persona.eta, persona.haMacchina);
            lastAction = () => DeletePersona(personaFInal.id);
            var isCorrectlyInserted= Persona.TryAdd(finalId, personaFInal);
            return (Persona.Values.ToImmutableList(),isCorrectlyInserted);

        }

        /// <inheritdoc/>
        public (ImmutableList<Persona>, bool) DeletePersona(int id)
        {
            var persona = ReadPersona(id);
            lastAction = () => InsertPersona(persona);
            var isCorrectlyInserted = false;
            if (Persona.ContainsKey(id))
            {
                isCorrectlyInserted=Persona.Remove(id);
            }
            
            return (Persona.Values.ToImmutableList(), isCorrectlyInserted);


        }

        /// <inheritdoc/>
        public (ImmutableList<Persona>, bool) UpdatePersona(Persona persona)
        {
            var personaO = ReadPersona(persona.id);
            lastAction = () => UpdatePersona(personaO);
            var isCorrectlyInserted = false;
            if (Persona.ContainsKey(persona.id))
            {
                Persona[persona.id] = persona;
                isCorrectlyInserted = true;
            }

            return (Persona.Values.ToImmutableList(), isCorrectlyInserted);

        }

        private Persona FindPersona(int id) => Persona.Where(x => x.Key == id).FirstOrDefault().Value ?? new Persona(id, YO_NON_ESISTO, YO_NON_ESISTO, id, false);

        /// <inheritdoc/>
        public void ReadFilePersona()
        {

            GetSnapshot().ToList().ForEach(x => Persona.TryAdd(x.Key, x.Value));
            globalId = Persona.Max(x => x.Key) + 1;
        }
        /// <inheritdoc/>
        public void WriteFilePersona()
        {
            using StreamWriter file = File.CreateText(congi.GetString(dbpath));
            JsonSerializer serializer = new JsonSerializer();
             serializer.Serialize(file, Persona.Values.ToList());

        }

        /// <inheritdoc/>
        public void RollBackToLastAction() => lastAction?.Invoke();

        /// <inheritdoc/>
        public Dictionary<int, Persona> GetSnapshot()
        {
            var persona = new Dictionary<int, Persona>();
            var serializer = new JsonSerializer();
            using StreamReader file = File.OpenText(congi.GetString(dbpath));
            using JsonTextReader reader = new JsonTextReader(file);
            var result = serializer.Deserialize<List<Persona>>(reader);
            if (result != null)
            {
                result.ForEach(x => persona.TryAdd(x.id, x));

            }
            return persona;
        }

        /// <inheritdoc/>
        public void SaveSnapshot(Dictionary<int, Persona> streamReader)
        {
            Persona.Clear();
            streamReader.ToList().ForEach(x => Persona.TryAdd(x.Key, x.Value));
            WriteFilePersona();
            globalId = Persona.Max(x => x.Key) + 1;
        }
    }
}
