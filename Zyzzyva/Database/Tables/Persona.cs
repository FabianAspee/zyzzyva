using System;
using System.Collections.Generic;

namespace Zyzzyva.Database.Tables
{
    /// <include file="Docs/Database/Tables/Persona.xml" path='docs/members[@name="persona"]/Persona/*'/>  
    public class Persona
    {
        ///<inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as Persona);

        ///<inheritdoc/>
        public bool Equals(Persona other) => other != null &&
                   id == other.id &&
                   nome == other.nome &&
                   cognome == other.cognome &&
                   eta == other.eta &&
                   haMacchina == other.haMacchina;

        ///<inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(id, nome, cognome, eta, haMacchina);

        ///<inheritdoc/>
        public override string ToString() => $"PERSONA - {id} {nome} {cognome} {eta} {haMacchina}";

        /// <include file="Docs/Database/Tables/Persona.xml" path='docs/members[@name="persona"]/Id/*'/>
        public readonly int id;
        /// <include file="Docs/Database/Tables/Persona.xml" path='docs/members[@name="persona"]/Nome/*'/>
        public readonly string nome;
        /// <include file="Docs/Database/Tables/Persona.xml" path='docs/members[@name="persona"]/Cognome/*'/>
        public readonly string cognome;
        /// <include file="Docs/Database/Tables/Persona.xml" path='docs/members[@name="persona"]/Eta/*'/>
        public readonly int eta;
        /// <include file="Docs/Database/Tables/Persona.xml" path='docs/members[@name="persona"]/HasCar/*'/>
        public readonly bool haMacchina;

        /// <include file="Docs/Database/Tables/Persona.xml" path='docs/members[@name="persona"]/PersonaConstructor/*'/>  
        public Persona(int id, string nome, string cognome, int eta, bool haMacchina)
        {
            this.id = id;
            this.nome = nome;
            this.cognome = cognome;
            this.eta = eta;
            this.haMacchina = haMacchina;
        }
        ///<inheritdoc/>
        public static bool operator ==(Persona left, Persona right) => EqualityComparer<Persona>.Default.Equals(left, right);
        ///<inheritdoc/>
        public static bool operator !=(Persona left, Persona right) => !(left == right);
    }
}
