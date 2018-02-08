DocumentDeleter

This contains code for a forms utility which can be used for deleting multiple documents from a partitioned collection of documentdb.

The following inputs are required for the forms application:

ConnectionString : Connection String for the DocumentDB database account

DatabaseId

CollectionId : CollectionId of partitioned collection

PartitionKey : Partition to delete the documents from

Field : filter field to filter documents within collection

Condition: filter condition

Value : filter value
