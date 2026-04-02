# Changelog

1.0.1
- Improved item distribution algorithm: transfers, hopper pulls, and pipe pushes are now asynchronous. This makes item distribution more predictable, especially when splitting throughput through hoppers.
- Unified item transfer rates based on material:
    - Bronze: 60 items/min (default)
    - Iron: 120 items/min (default)
    - Both are now fully configurable in the config file.
- Added support for more machines (hoppers and pipes can now push items to these):
    - piece_oven (Vanilla Oven)
    - RDP_beehive (Producers mod Beehive)
- Added pulling support for hoppers to grab items from:
    - BCP_ClayCollector (FineWoodPieces mod Clay Collector)
    - RDP_beehive (Producers mod Beehive)
    - piece_oven (Vanilla Oven)
    - windmill (Vanilla Windmill)

1.0.0
- Release, forked from ValheimHopper 2.0.0
