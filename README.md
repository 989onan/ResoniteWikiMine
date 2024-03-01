# ResoniteWikiMine

This is a tool to automate busywork for updating the [Resonite Wiki](https://wiki.resonite.com/Main_Page). Automatically generating new component pages, that kinda stuff.

# Architecture

This project contains a bunch of "commands" that do various data processing operations between Resonite's code, the wiki's API, and a cache/scratch space SQLite database.

For extracting data about Resonite, the game assembly files (e.g. `FrooxEngine.dll`) are directly referenced. It's just .NET, it works. We can then just directly call into various FrooxEngine APIs to retrieve information.

The database is used as a cache to avoid having to hit the wiki API all the damn time, so I can run commands to test new generation of pages without worrying about making Prime notice me. Results are output into the same database, which makes it practical for me to browse results of generation in an SQLite database viewer, with all the advantages that gives.

