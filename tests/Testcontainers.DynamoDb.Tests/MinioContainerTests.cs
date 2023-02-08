﻿namespace Testcontainers.DynamoDb;

public sealed class MinioContainerTest : IAsyncLifetime
{
  private readonly DynamoDbContainer dynamoDbContainer = new DynamoDbBuilder().Build();

  public Task InitializeAsync()
  {
    return this.dynamoDbContainer.StartAsync();
  }

  public Task DisposeAsync()
  {
    return this.dynamoDbContainer.DisposeAsync().AsTask();
  }

  [Fact]
  [Trait(nameof(DockerCli.DockerPlatform), nameof(DockerCli.DockerPlatform.Linux))]
  public async Task CreateTableReturnsHttpStatusCodeOk()
  {
    // Given
    var clientConfig = new AmazonDynamoDBConfig();
    clientConfig.ServiceURL = this.dynamoDbContainer.GetEndpoint();
    clientConfig.UseHttp = true;
    using var client = new AmazonDynamoDBClient(new BasicAWSCredentials("dummy", "dummy"), clientConfig);

    // When
    var tableResponse = await client.CreateTableAsync(new CreateTableRequest()
      {
        TableName = "TestDynamoDbTable",
        AttributeDefinitions = new List<AttributeDefinition>() { new AttributeDefinition("Id", ScalarAttributeType.S), new AttributeDefinition("Name", ScalarAttributeType.S), },
        KeySchema = new List<KeySchemaElement>() { new KeySchemaElement("Id", KeyType.HASH), new KeySchemaElement("Name", KeyType.RANGE), },
        ProvisionedThroughput = new ProvisionedThroughput(1, 1),
        TableClass = TableClass.STANDARD,
      })
      .ConfigureAwait(false);

    // Then
    Assert.Equal(HttpStatusCode.OK, tableResponse.HttpStatusCode);
  }

  [Fact]
  [Trait(nameof(DockerCli.DockerPlatform), nameof(DockerCli.DockerPlatform.Linux))]
  public async Task InsertElementToTableReturnsInsertedElement()
  {
    // Given
    var tableName = $"TestDynamoDbTable-{Guid.NewGuid():D}";
    var itemId = Guid.NewGuid().ToString("D");

    var clientConfig = new AmazonDynamoDBConfig();
    clientConfig.ServiceURL = this.dynamoDbContainer.GetEndpoint();
    clientConfig.UseHttp = true;
    using var client = new AmazonDynamoDBClient(new BasicAWSCredentials("dummy", "dummy"), clientConfig);

    // When
    _ = await client.CreateTableAsync(new CreateTableRequest()
      {
        TableName = tableName,
        AttributeDefinitions = new List<AttributeDefinition>() { new AttributeDefinition("Id", ScalarAttributeType.S), new AttributeDefinition("Name", ScalarAttributeType.S), },
        KeySchema = new List<KeySchemaElement>() { new KeySchemaElement("Id", KeyType.HASH), new KeySchemaElement("Name", KeyType.RANGE), },
        ProvisionedThroughput = new ProvisionedThroughput(1, 1),
        TableClass = TableClass.STANDARD,
      })
      .ConfigureAwait(false);

    var response = await client.PutItemAsync(new PutItemRequest(tableName, new Dictionary<string, AttributeValue>() { { "Id", new AttributeValue() { S = itemId } }, { "Name", new AttributeValue() { S = "Test" } } }));
    Assert.Equal(HttpStatusCode.OK, response.HttpStatusCode);

    var getItemResponse = await client.GetItemAsync(new GetItemRequest(tableName, new Dictionary<string, AttributeValue>() { { "Id", new AttributeValue() { S = itemId } }, { "Name", new AttributeValue() { S = "Test" } } }, true));

    // Then
    Assert.Equal(HttpStatusCode.OK, getItemResponse.HttpStatusCode);
  }
}
