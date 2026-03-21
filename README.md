# Umbraco Search - a developer's perspective

This repo contains the complete source code from my talk _Umbraco Search - a developer's perspective_ at [Umbraco Spark 26](https://umbracospark.com)

## Prerequisites

You should probably have attended the talk for most of this to make any sense 🙈

## Running the demo

Since the demo uses the [search provider for Elastic Search](https://github.com/kjac/Kjac.SearchProvider.Elasticsearch), you'll need a running Elasticsearch instance to run the demo successfully. Check the link for configuration instructions.

That being said, the parts of the demo that target Examine can still run without without an Elasticsearch instance. Just disregard the myriad of runtime errors 😄

The repo contains the Umbraco DB, so it can run right off the bat. The admin credentials to the Umbraco backoffice are:

- Username: admin@localhost
- Password: SuperSecret123
