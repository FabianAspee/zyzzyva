using System.Collections.Generic;
using System.Collections.Immutable;

namespace Zyzzyva.Database.Tables
{
    /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/IPersonaCRUD/*'/>
    public interface IPersonaCRUD
    {
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/ReadAllPersone/*'/>
        public ImmutableList<Persona> ReadAllPersone();
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/ReadPersona/*'/>
        public Persona ReadPersona(int id);
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/InsertPersona/*'/>
        public (ImmutableList<Persona>, bool) InsertPersona(Persona persona);
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/DeletePersona/*'/>
        public (ImmutableList<Persona>, bool) DeletePersona(int id);
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/UpdatePersona/*'/>
        public (ImmutableList<Persona>, bool) UpdatePersona(Persona persona);
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/ReadFilePersona/*'/>
        public void ReadFilePersona();
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/WriteFilePersona/*'/>
        public void WriteFilePersona();
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/RollBackToLastAction/*'/>
        public void RollBackToLastAction();
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/GetSnapshot/*'/>
        public Dictionary<int, Persona> GetSnapshot();
        /// <include file="Docs/Database/Tables/DBOperations/PersonaOperations/IPersonaCRUD.xml" path='docs/members[@name="ipersona"]/SaveSnapshot/*'/>
        void SaveSnapshot(Dictionary<int, Persona> streamReader);
    }
}
