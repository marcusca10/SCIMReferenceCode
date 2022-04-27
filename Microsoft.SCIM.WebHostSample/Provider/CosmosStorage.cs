using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.SCIM.WebHostSample.Provider
{
    public class CosmosStorage
    {
        private readonly IConfiguration _config;

        internal CosmosClient cosmosClient;
        internal Database database;
        internal Container container;

        public CosmosStorage()
        {
            //Initialization = InitializeAsync();
        }

        private static readonly CosmosStorage InstanceValue = new CosmosStorage();

        public static CosmosStorage Instance => CosmosStorage.InstanceValue;

        public Task Initialization { get; private set; }

        public async Task InitializeAsync()
        {
            //cosmosClient = new CosmosClient(_config.GetConnectionString("HrScimEndpoint"));
            cosmosClient = new CosmosClient("AccountEndpoint=https://hrscim-database.documents.azure.com:443/;AccountKey=KDXiCmjTOIIqrFOOgjOHiRZ2NJZCr5pQO2QfUMWYusU1fxIy7WtFjyefNyzAi2vgFW2d4JXpa4Lx2IBufnLpgQ==;");

            // Create a new database
            this.database = await cosmosClient.CreateDatabaseIfNotExistsAsync("HrScimDatabse");

            // Create new containers
            this.container = await this.database.CreateContainerIfNotExistsAsync("ResourceContainer", "/id");
            //this.container = await this.database.CreateContainerIfNotExistsAsync("UserContainer", "/id");
            //this.container = await this.database.CreateContainerIfNotExistsAsync("GroupContainer", "/id");
        }

        public async Task Create(Resource resource)
        {
            try
            {
                // Create an item in the container
                if (resource is Core2EnterpriseUser)
                {
                    ItemResponse<Core2EnterpriseUser> response = await this.container.CreateItemAsync<Core2EnterpriseUser>(resource as Core2EnterpriseUser, new PartitionKey(resource.Identifier));
                    // Note that after creating the item, we can access the body of the item with the Resource property of the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", response.Resource.Identifier, response.RequestCharge);
                }
                else if (resource is Core2Group)
                {
                    ItemResponse<Core2Group> response = await this.container.CreateItemAsync<Core2Group>(resource as Core2Group, new PartitionKey(resource.Identifier));
                    // Note that after creating the item, we can access the body of the item with the Resource property of the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                    Console.WriteLine("Created item in database with id: {0} Operation consumed {1} RUs.\n", response.Resource.Identifier, response.RequestCharge);
                }
                else
                    throw new Exception();

            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                Console.WriteLine("Item in database with id: {0} already exists\n", resource.Identifier);
            }
            catch (CosmosException ex)
            {
                Console.WriteLine("Error creating item in database with id: {0} \n\tCode {1}: {2}\n", resource.Identifier,ex.StatusCode, ex.Message);
            }
        }

        public async Task<List<Resource>> Read(string query)
        {
            Console.WriteLine("Running query: {0}\n", query);

            List<Resource> resources = new List<Resource>();
            QueryDefinition queryDefinition = new QueryDefinition(query);

            try
            {
                FeedIterator<Core2EnterpriseUser> resultset = this.container.GetItemQueryIterator<Core2EnterpriseUser>(queryDefinition);

                while (resultset.HasMoreResults)
                {
                    FeedResponse<Core2EnterpriseUser> response = await resultset.ReadNextAsync();
                    Console.WriteLine("Result itens count: {0} Operation consumed {1} RUs.\n", response.Count, response.RequestCharge);

                    foreach (Resource item in response)
                    {
                        resources.Add(item);
                    }
                }
            }
            catch (CosmosException ex)
            {
                Console.WriteLine("Error reading from database with query: {0}\n\tCode {1}: {2}\n", query, ex.StatusCode, ex.Message);
            }

            return resources;
        }

        public async Task Delete(string identifier)
        {
            try
            {
                // Delete an item. Note we must provide the partition key value and id of the item to delete
                ItemResponse<Resource> response = await this.container.DeleteItemAsync<Resource>(identifier, new PartitionKey(identifier));
                // Note that after creating the item, we can access the body of the item with the Resource property of the ItemResponse. We can also access the RequestCharge property to see the amount of RUs consumed on this request.
                Console.WriteLine("Deleted item {0} Operation consumed {1} RUs.\n", response.Resource.Identifier, response.RequestCharge);
            }
            catch (CosmosException ex)
            {
                Console.WriteLine("Error deleting item in database with id: {0} [{1}]\n", identifier, ex.Message);
            }
        }






        public async Task<bool> Exists(string id)
        {
            int count = 0;

            QueryDefinition query = new QueryDefinition("SELECT value count(1) FROM c WHERE c.id = '@id'")
                .WithParameter("@id", id);

            try
            {
                using (FeedIterator<int> resultset = container.GetItemQueryIterator<int>(query))
                {
                    while (resultset.HasMoreResults)
                    {
                        FeedResponse<int> response = await resultset.ReadNextAsync();
                        Console.WriteLine("'Exists' took {0} ms. RU consumed: {1}, Number of items : {2}", response.Diagnostics.GetClientElapsedTime().TotalMilliseconds, response.RequestCharge, response.Count);

                        foreach (int item in response)
                        {
                            count += item;
                        }
                    }
                }
            }
            catch (CosmosException ex)
            {
                Console.WriteLine("Error reading from database with query: {0}\n\tCode {1}: {2}\n", query.QueryText, ex.StatusCode, ex.Message);
            }

            return count > 0;
        }

        public async Task<bool> UserNameExists(string username)
        {
            int count = 0;

            QueryDefinition query = new QueryDefinition("SELECT value count(1) FROM c WHERE c.userName = @userName")
                .WithParameter("@userName", username);

            using (FeedIterator<int> resultset = container.GetItemQueryIterator<int>(query))
            {
                while (resultset.HasMoreResults)
                {
                    FeedResponse<int> response = await resultset.ReadNextAsync();
                    Console.WriteLine("'UserNameExists' took {0} ms. RU consumed: {1}, Number of items : {2}", response.Diagnostics.GetClientElapsedTime().TotalMilliseconds, response.RequestCharge, response.Count);

                    foreach (int item in response)
                    {
                        count += item;
                    }
                }
            }

            return count > 0;
        }
    }
}
