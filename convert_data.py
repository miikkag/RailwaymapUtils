import sys
import sqlite3
import os
import xml
import xml.sax
import sys
from enum import Enum, auto


# ANSI codes to windows
os.system("color")

CLEAR = "\033[1K\033[1G"


targets = {}

targets['--border'] = "border"
targets['--coastline'] = "coastline"
targets['--railway'] = "railway"
targets['--lightrail'] = "lightrail"
targets['--lakes'] = "lakes"
targets['--water'] = "water"


use_targets = []


class node:
	def __init__(self,set_id, set_lat, set_lon):
		self.attr_id = set_id
		self.attr_lat = set_lat
		self.attr_lon = set_lon
		self.railwaystation = 0
		self.tags = []

class way:
	def __init__(self,set_id):
		self.attr_id = set_id
		self.points = []
		self.railway = False
		self.tags = []

class relation:
	def __init__(self,set_id):
		self.attr_id = set_id
		self.members = []
		self.tags = []

class osmst(Enum):
	ROOT=auto()
	OSM=auto()
	NODE=auto()
	NODE_TAG=auto()
	WAY=auto()
	WAY_ND=auto()
	WAY_TAG=auto()
	RELATION=auto()
	RELATION_MEMBER=auto()
	RELATION_TAG=auto()
	OTHER=auto()


class OSMNodesHandler ( xml.sax.ContentHandler ):
	def __init__(self):
		self.elementcount = 0
		self.state = osmst.ROOT
		self.unknown_level = 0
		self.nodecount = 0
		self.waycount = 0
		self.relcount = 0
		self.way = way(0)
		self.relation = relation(0)
		self.node = node(0,0,0)

	def PrintStatus(self):
		sys.stdout.write(f"\rElements: {self.elementcount}  Nodes: {self.nodecount}  Ways: {self.waycount}  Relations: {self.relcount}")
		sys.stdout.flush()

	def startElement(self, tag, attributes):
		global c

		if self.state == osmst.OSM:
			if tag=="node":
				## NODE
				self.node = node (attributes.getValue("id"), attributes.getValue("lat"), attributes.getValue("lon"))
				self.state = osmst.NODE
				self.nodecount += 1

			elif tag=="way":
				## WAY
				self.way = way(attributes.getValue("id"))
				self.state = osmst.WAY
				self.waycount += 1

			elif tag=="relation":
				self.relation = relation(attributes.getValue("id"))
				self.state = osmst.RELATION
				self.relcount += 1

			else:
				## other element -- ignore
				self.unknown_level += 1
				self.state = osmst.OTHER
				print(f"Found other: {tag}")

		elif self.state == osmst.NODE:
			if tag=="tag":
				attr_k = attributes.getValue("k")
				attr_v = attributes.getValue("v")
				self.node.tags.append( ( attr_k, attr_v ) )
				if (attr_k== "railway"):
					if(attr_v=="station"):
						self.node.railwaystation = 1
					elif (attr_v=="site"):
						self.node.railwaystation = 2
					elif (attr_v=="yard"):
						self.node.railwaystation = 3
				self.state = osmst.NODE_TAG
			else:
				print ("Unknown element inside NODE: " + tag)
				self.unknown_level += 1

		elif self.state == osmst.WAY:
			if tag=="nd":
				## save way node
				self.way.points.append(attributes.getValue("ref"))
				self.state = osmst.WAY_ND
			elif tag=="tag":
				## save way tag
				attr_k = attributes.getValue("k")
				attr_v = attributes.getValue("v")
				if (attr_k=="railway"):
					if (attr_v=="rail") or (attr_v=="abandoned") or (attr_v=="disused") or (attr_v=="narrow_gauge"):
						self.way.railway = True
				self.way.tags.append( (attr_k, attr_v) )
				self.state = osmst.WAY_TAG
			else:
				print ("Unknown element inside WAY: " + tag)
				self.unknown_level += 1

		elif self.state == osmst.RELATION:
			if tag=="member":
				if attributes.getValue("type")=="way":
					self.relation.members.append((attributes.getValue("ref"),attributes.getValue("role")))
				self.state = osmst.RELATION_MEMBER
			elif tag=="tag":
				self.relation.tags.append ( (attributes.getValue("k"), attributes.getValue("v")) )
				self.state = osmst.RELATION_TAG
			else:
				print ("Unknown element inside RELATION: " + tag)
				self.unknown_level += 1

		elif self.state == osmst.ROOT:
			if tag=="osm":
				print ("Start OSM")
				self.state=osmst.OSM
			else:
				print("Unexpected root element " + tag)
				self.unknown_level += 1
				self.state = osmst.OTHER

		else:
			self.unknown_level += 1

		self.elementcount += 1

		#if (self.elementcount % 20000)==0:
		#	self.PrintStatus()

	def endElement(self, tag):
		global c

		if self.unknown_level > 0:
			self.unknown_level -= 1

		if self.unknown_level == 0:
			if self.state == osmst.NODE:
				# Save node and tags
				c.execute ( "INSERT OR IGNORE INTO nodes ( id, lat, lon, station ) VALUES ( ?, ?, ?, ? );", ( self.node.attr_id, self.node.attr_lat, self.node.attr_lon, self.node.railwaystation ) )
				for t in self.node.tags:
					c.execute ( "INSERT INTO node_tags ( node_id, k, v ) VALUES ( ?, ?, ? );", ( self.node.attr_id, t[0], t[1] ) )

				self.state = osmst.OSM

			elif self.state == osmst.NODE_TAG:
				self.state = osmst.NODE

			elif self.state == osmst.WAY:
				# Save way
				c.execute ( "INSERT OR IGNORE INTO ways ( id, railway ) VALUES ( ?, ? );", ( self.way.attr_id, self.way.railway ) )
				for pt in self.way.points:
					c.execute ( "INSERT INTO way_nodes ( way_id, node_id ) VALUES ( ?, ? );", ( self.way.attr_id, pt ) )
				for t in self.way.tags:
					c.execute ( "INSERT INTO way_tags ( way_id, k, v ) VALUES ( ?, ?, ? );", ( self.way.attr_id, t[0], t[1] ) )

				self.state = osmst.OSM

			elif self.state == osmst.OTHER:
				self.state = osmst.OSM

			elif self.state == osmst.WAY_ND:
				self.state = osmst.WAY

			elif self.state == osmst.WAY_TAG:
				self.state = osmst.WAY

			elif self.state == osmst.RELATION:
				# Save relation
				c.execute ( "INSERT INTO relations ( id ) VALUES ( ? );", ( self.relation.attr_id, ) )
				for m in self.relation.members:
					c.execute ( "INSERT INTO relation_members ( relation_id, way_id, role ) VALUES ( ?, ?, ? );", ( self.relation.attr_id, m[0], m[1] ) )
				for t in self.relation.tags:
					c.execute ( "INSERT INTO relation_tags ( relation_id, k, v ) VALUES ( ?, ?, ? );", ( self.relation.attr_id, t[0], t[1] ) )

				self.state = osmst.OSM

			elif self.state == osmst.RELATION_MEMBER:
				self.state = osmst.RELATION

			elif self.state == osmst.RELATION_TAG:
				self.state = osmst.RELATION

			elif self.state == osmst.OSM:
				if tag=="osm":
					print ("\nEnd OSM")
					self.state=osmst.ROOT
				else:
					print ("Unexpected OSM end tag " + tag)

# =======================================================================================

if len(sys.argv) <= 1:
	exit(f"Arguments: country [ {' | '.join(targets)} ]")
elif len(sys.argv) == 2:
	for k in targets:
		use_targets.append(targets[k])
else:
	for a in range (2, len(sys.argv)):
		if sys.argv[a] in targets:
			use_targets.append(targets[sys.argv[a]])
		else:
			exit (f"Argument {sys.argv[a]} unknown")

bbox = ""
bbox_name = sys.argv[1]

with open(os.path.join("Data",bbox_name,"bbox.txt")) as f:
	line = f.readline().rstrip()
	if line.startswith('('):
		bbox = line
	else:
		exit (f"Invalid line in bbox.txt: {line}")

if len(bbox)==0:
	exit(f"No bbox found for {sys.argv[1]}")

aborted = False

for t in use_targets:
	print(f"\nProcessing target {t}")
	filename_osm = os.path.join("Data", bbox_name, f"{t}.xml")
	filename_db  = os.path.join("Data", bbox_name, f"{t}.db")
	
	if os.path.exists(filename_db):
		os.remove(filename_db)
	
	conn = sqlite3.connect(filename_db)
	
	c = conn.cursor()
	
	c.execute ( "CREATE TABLE nodes ( id INTEGER PRIMARY KEY, lat REAL, lon REAL, station INTEGER );" )
	c.execute ( "CREATE TABLE node_tags ( id INTEGER PRIMARY KEY, node_id INT, k TEXT, v TEXT );" )
	
	c.execute ( "CREATE TABLE ways ( id INTEGER PRIMARY KEY, railway BOOLEAN );" )
	c.execute ( "CREATE TABLE way_nodes ( id INTEGER PRIMARY KEY, way_id INT, node_id INT );" )
	c.execute ( "CREATE TABLE way_tags ( id INTEGER PRIMARY KEY, way_id INT, k TEXT, v TEXT );" )
	
	c.execute ( "CREATE TABLE relations ( id INTEGER );" )
	c.execute ( "CREATE TABLE relation_members ( id INTEGER PRIMARY KEY, relation_id INT, way_id INT, role TEXT );" )
	c.execute ( "CREATE TABLE relation_tags ( id INTEGER PRIMARY KEY, relation_id INT, k TEXT, v TEXT );" )

	parser = xml.sax.make_parser()
	parser.setFeature(xml.sax.handler.feature_namespaces, 0)
	
	handler = OSMNodesHandler()
	parser.setContentHandler( handler )
		
	try:
		parser.parse(filename_osm)
	except KeyboardInterrupt:
		print ("Aborted.")
		aborted = True
	
	c.execute ( "COMMIT;")
	
	print ("PARSE OSM:  elements=" + str(handler.elementcount) + "  nodes=" + str(handler.nodecount) + "  ways=" + str(handler.waycount) + "  relations=" + str(handler.relcount))
	
	print (f"{CLEAR}Creating index 1...", end='')
	c.execute ( "CREATE INDEX node_tags_index ON node_tags ( node_id );" )
	print (f"{CLEAR}Creating index 2...", end='')
	c.execute ( "CREATE INDEX way_nodes_index ON way_nodes ( way_id );" )
	print (f"{CLEAR}Creating index 3...", end='')
	c.execute ( "CREATE INDEX way_tags_index ON way_tags ( way_id );" )
	print (f"{CLEAR}Creating index 4...", end='')
	c.execute ( "CREATE INDEX relation_members_index ON relation_members ( relation_id );" )

	print (f"{CLEAR}Creating index 4...", end='')
	c.execute ( "CREATE INDEX node_idx ON nodes ( id );" )
	
	print("")
	
	conn.close()
	
	if aborted:
		break

