using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//D365 Specific Assemblies
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Sample_Plugin_With_CRUD
{
    public class samplecodes : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            //Tracing Object
            ITracingService tracing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

            //ExecutionContext Object
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            //OrganizationServiceFactory Object
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

            //OrganizationService Object
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
            {

                // Obtain the target entity from the input parameters.
                Entity entity = (Entity)context.InputParameters["Target"];

                if (entity.LogicalName != "account")
                    return;

                try
                {
                    //Create contact record
                    Entity cont = new Entity("contact");
                    cont["firstname"] = "John";
                    cont["lastname"] = "Bedford";
                    Guid contId = service.Create(cont);

                    //Retrieve contact record
                    Entity contData = service.Retrieve("contact", contId, new ColumnSet("firstname", "lastname"));

                    //update contact
                    Entity updatedContact = new Entity("contact");
                    updatedContact.Id = contId;
                    updatedContact["jobtitle"] = "Director";
                    updatedContact["emailaddress1"] = "director@test.com";
                    service.Update(updatedContact);

                    //Delete Record
                    service.Delete("contact", contId);

                    //Retrieve multiple with FetchXML
                    // Retrieve all accounts owned by the user with read access rights to the accounts and   
                    // where the last name of the user is not Cannon.   
                    string fetch = @" <fetch mapping='logical'>  
                                         <entity name='account'>   
                                            <attribute name='accountid'/>   
                                            <attribute name='name'/>   
                                            <link-entity name='systemuser' to='owninguser'>   
                                               <filter type='and'>   
                                                  <condition attribute='lastname' operator='ne' value='Bedford' />   
                                               </filter>   
                                            </link-entity>   
                                         </entity>   
                                       </fetch> ";

                    EntityCollection result = service.RetrieveMultiple(new FetchExpression(fetch));

                    foreach (var c in result.Entities)
                    {
                        //write your logic
                        System.Console.WriteLine(c.Attributes["name"]);
                    }

                    //Retrieve multiple with Query Expression
                    // Retrieve contact with first name as Alexa and last name as kenny
                    ConditionExpression condition1 = new ConditionExpression();
                    condition1.AttributeName = "lastname";
                    condition1.Operator = ConditionOperator.Equal;
                    condition1.Values.Add("kenny");

                    ConditionExpression condition2 = new ConditionExpression();
                    condition2.AttributeName = "firstname";
                    condition2.Operator = ConditionOperator.Equal;
                    condition2.Values.Add("Alexa");

                    FilterExpression filter1 = new FilterExpression();
                    filter1.Conditions.Add(condition1);
                    filter1.Conditions.Add(condition2);

                    QueryExpression query = new QueryExpression("contact");
                    query.ColumnSet.AddColumns("firstname", "lastname");
                    query.Criteria.AddFilter(filter1);

                    EntityCollection result1 = service.RetrieveMultiple(query);
                    foreach (var a in result1.Entities)
                    {
                        //write your logic
                        Console.WriteLine("Name: " + a.Attributes["firstname"] + " " + a.Attributes["lastname"]);
                    }

                    //execute method 
                    Entity acc = new Entity("account");
                    acc["name"] = "Test Account";
                    CreateRequest req = new CreateRequest();
                    req.Target = acc;
                    CreateResponse response = (CreateResponse)service.Execute(req);

                   
                    //execute multiple method 
                    QueryExpression queryacc = new QueryExpression("account")
                    {
                        ColumnSet = new ColumnSet("accountname", "createdon"),
                    };

                    //Obtain the results of previous QueryExpression
                    EntityCollection results = service.RetrieveMultiple(queryacc);

                    if (results != null && results.Entities != null && results.Entities.Count > 0)
                    {
                        ExecuteMultipleRequest batch = new ExecuteMultipleRequest();
                        foreach (Entity account in results.Entities)
                        {
                            account.Attributes["accountname"] += "_UPDATED";

                            UpdateRequest updateRequest = new UpdateRequest();
                            updateRequest.Target = account;

                            batch.Requests.Add(updateRequest);
                        }
                        service.Execute(batch);
                    }



                }
                catch (Exception ex)
                {
                    throw new InvalidPluginExecutionException(ex.Message);
                }
            }
        }
    }
}
