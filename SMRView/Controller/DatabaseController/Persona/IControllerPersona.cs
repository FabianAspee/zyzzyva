using System.Threading.Tasks;
using ZyzzyvagRPC.Services;

namespace SMRView.Controller.ControllerContract
{
    /// <include file="Docs/Controller/DatabaseController/Persona/IControllerPersona.xml" path='docs/members[@name="icontrollerpersona"]/IControllerPersona/*'/>
    public interface IControllerPersona
    {
        /// <include file="Docs/Controller/DatabaseController/Persona/IControllerPersona.xml" path='docs/members[@name="icontrollerpersona"]/Read/*'/>
        Task Read(int id);

        /// <include file="Docs/Controller/DatabaseController/Persona/IControllerPersona.xml" path='docs/members[@name="icontrollerpersona"]/ReadAll/*'/>
        Task ReadAll();
        /// <include file="Docs/Controller/DatabaseController/Persona/IControllerPersona.xml" path='docs/members[@name="icontrollerpersona"]/Insert/*'/>
        Task Insert(PersonagRPC persona);
        /// <include file="Docs/Controller/DatabaseController/Persona/IControllerPersona.xml" path='docs/members[@name="icontrollerpersona"]/Update/*'/>
        Task Update(PersonagRPC persona);
        /// <include file="Docs/Controller/DatabaseController/Persona/IControllerPersona.xml" path='docs/members[@name="icontrollerpersona"]/Delete/*'/>
        Task Delete(int id);
        /// <include file="Docs/Controller/DatabaseController/Persona/IControllerPersona.xml" path='docs/members[@name="icontrollerpersona"]/Byzantine/*'/>
        Task<SetByzantineResponse> Byzantine(int id);
    }
}
