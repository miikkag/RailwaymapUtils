import os
import sys
import requests
import time

# ANSI codes to windows
os.system("color")

CLEAR = "\033[1K\033[1G"


targets = {}

targets['--border'] = ( "border",
	[ 'way["admin_level"="2"][!"maritime"]' ] )

targets['--coastline'] = ( "coastline",
	{ 'way["natural"="coastline"]' } )

targets['--railway'] = ( "railway",
	{ 'way["railway"="rail"]',
	  'way["construction"="rail"]',
	  'way["railway"="narrow_gauge"]',
	  'way["construction"="narrow_gauge"]',
	  'node["railway"="station"]["station"!="subway"]["station"!="light_rail"]',
	  'node["railway"="site"]',
	  'node["railway"="yard"]'
      } )

#targets['--railwaysites'] = ( "railway-sites",
#	  { 'node["railway"="yard"]',
#	    'node["railway"="site"]' } )

targets['--lightrail'] = ( "lightrail",
	{ 'way["railway"="light_rail"]', 'way["construction"="light_rail"]',
	  'node["railway"="station"]["station"="light_rail"]' } )

targets['--lakes'] = ( "lakes",
	{ 'rel["natural"="water"]["water"="lake"]' } )


#targets['--water'] = ( "water",
#	{ 'way["natural"="water"]' } )


use_targets = []

endpoint = "https://lz4.overpass-api.de/api/interpreter"


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


for i,t in enumerate(use_targets):
	if i>0:
		for s in range(33,0,-1):
			print(f"{CLEAR}Sleeping: {s}...", end='')
			sys.stdout.flush()
			time.sleep(1)
		print(f"{CLEAR}")
	filename = os.path.join("Data", bbox_name, f"{t[0]}.xml")
	query = "[out:xml][timeout:2000];\n"
	query = query + "(\n"
	for l in t[1]:
		query = query + f"  {l}{bbox};\n"
	query = query + ");\n"
	query = query + "out;\n>;\nout skel qt;\n"

	print(f"Request: {t[0]}")

	print("Query:")
	print(query)

	# Make request
	with requests.get(endpoint, data=query, stream=True) as r:
		r.raise_for_status()
		datalen = 0
		with open(filename, "wb") as f:
			for chunk in r.iter_content(chunk_size=64*1024):
				f.write(chunk)
				datalen += len(chunk)
				print(f"{CLEAR}Received {round(datalen/(1024*1024))} MB", end='')
				sys.stdout.flush()
		if datalen < (1024*1024):
			print (f"{CLEAR}Received {datalen} bytes, saved as {filename}")
		else:
			print (f"{CLEAR}Received: {round(datalen/(1024*1024))} MB data, saved as {filename}")
