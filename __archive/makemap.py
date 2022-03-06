import sqlite3
import os
import sys
import math
import drawline
import haversine
from PIL import Image,ImageDraw,ImageFont


def merc_x(lon):
	r_major=6378137.000
	return r_major*math.radians(lon)

def merc_y(lat):
	if lat>89.5:lat=89.5
	if lat<-89.5:lat=-89.5
	r_major=6378137.000
	r_minor=6356752.3142
	temp=r_minor/r_major
	eccent=math.sqrt(1-temp**2)
	phi=math.radians(lat)
	sinphi=math.sin(phi)
	con=eccent*sinphi
	com=eccent/2
	con=((1.0-con)/(1.0+con))**com
	ts=math.tan((math.pi/2-phi)/2)/con
	y=0-r_major*math.log(ts)
	return y

def CoordDiff(pt1, pt2):
	return abs(pt1[0]-pt2[0])+abs(pt1[1]-pt2[1])

def PrintStatus(num, maxval):
	sys.stdout.write("\rItem " + str(num) + "/" + str(maxval))
	sys.stdout.flush()

class Bounds:
	def __init__(self,LAT_MIN, LAT_MAX, LON_MIN, LON_MAX):
		self.lat_min = LAT_MIN
		self.lat_max = LAT_MAX
		self.lon_min = LON_MIN
		self.lon_max = LON_MAX

class RailwaySet:
	def __init__(self, voltage):
		self.voltage = voltage
		self.ways_single = []
		self.ways_double = []
		self.coordinates_single = []
		self.coordinates_double = []

	def PutItem(self, way_id, tracks):
		if tracks>1:
			self.ways_double.append(way_id)
		else:
			self.ways_single.append(way_id)

	def Process(self):
		self.coordinates_single = Process_WayIDs ( self.ways_single, "railway " + str(self.voltage) + ".1" )
		self.coordinates_double = Process_WayIDs ( self.ways_double, "railway " + str(self.voltage) + ".2" )


def ScanBounds(way_coordset, print_name):
	lat_max = -9999.0
	lat_min = 9999.0
	lon_max = -9999.0
	lon_min = 9999.0

	print ("Scanning bounds (" + print_name + "):")
	maxval = len(way_coordset)
	interval = int(maxval/100)+1
	i = 0

	for ws in way_coordset:
		if (i%interval)==0:
			PrintStatus(i, maxval)
		i+=1
		for c in ws:
			if c[0] > lat_max:
				lat_max = c[0]
			if c[0] < lat_min:
				lat_min = c[0]
			if c[1] > lon_max:
				lon_max = c[1]
			if c[1] < lon_min:
				lon_min = c[1]

	print("")
	return Bounds ( lat_min, lat_max, lon_min, lon_max )


def RailwayLineStyle(voltage, tracks):
	if voltage==0:
		# non-electrified
		color=(40,220,40)
	elif voltage==1500:
		color=(205,135,90)
	elif voltage==3000:
		color=(0,200,200)
	elif voltage==15000:
		color=(255,0,0)
	elif voltage==25000:
		color=(0,120,255)
	else:
		color=(255,0,224)

	if tracks>1:
		width=3
	else:
		width=1

	return (color, width)


def Process_WayIDs(way_id_list, print_name):
	way_sets = []
	print ("Processing way ids ("+print_name+"):")
	maxval = len(way_id_list)
	interval = int(maxval/100)+1
	i = 0

	for wi in way_id_list:
		if (i%interval)==0:
			PrintStatus(i, maxval)
		i+=1
		query_result = c.execute ( "SELECT node_id FROM way_nodes WHERE way_id=?;", ( wi, ) )
		tmp = []
		for qr in query_result:
			tmp.append (qr[0])
		way_sets.append (tmp)

	print("")

	print ("Processing way sets ("+print_name+"):")
	maxval = len(way_sets)
	interval = int(maxval/100)+1
	i = 0

	result_cordinateset = []

	for ws in way_sets:
		if (i%interval)==0:
			PrintStatus(i, maxval)
		i+=1
		tmp = []
		for n in ws:
			c.execute ( "SELECT lat, lon FROM nodes WHERE id=?", ( n, ) )
			tmp_row = c.fetchone()
			if tmp_row != None:
				tmp.append(tmp_row)
			else:
				print("No data found for node id", n, "in wayset" )
		result_cordinateset.append(tmp)

	print("")
	
	return result_cordinateset



def Draw_Way_Coordinates(ways, gfx, img, lineattrs, print_name):
	colors = [ (0,0,0), (255,0,0), (0,255,0), (0,0,255), (128,0,0), (0,128,0), (0,0,128), ( 255,128,0), (0,255,128), (128,0,255) ]
	usefilter = 3
	if lineattrs[1] > 1:
		usefilter=5
	print("Drawing ways (" + print_name + "):")
	maxval = len(ways)
	interval = int(maxval/100)+1
	i = 0
	for w in ways:
		if (i%interval)==0:
			PrintStatus(i, maxval)
		set_lines = []
		prev_x = -999
		prev_y = -999
		for b in range (len(w)):
			y = scale * ( y_max - merc_y(w[b][0]))
			x = scale * ( merc_x(w[b][1]) - x_min)
			# filter lines too close to each other
			dist = abs(x-prev_x) + abs(y-prev_y)
			if dist > usefilter or b==len(w)-1:
				set_lines.append(( x, y ))
				prev_x = x
				prev_y = y
		if lineattrs[1] > 1:
			drawline.draw_polyline3(img, lineattrs[0], set_lines)
		else:
			usecolor = (0,0,0)
			if print_name=="waters":
				usecolor = colors[i%len(colors)]
			else:
				usecolor = lineattrs[0]

			gfx.line(set_lines, lineattrs[0], 1)
		i+=1

	print ("")


def Draw_Way_Polygon(ways, gfx, img, fillcolor, min_size, print_name):
	usefilter = 3
	print("Drawing ways (" + print_name + "):")
	maxval = len(ways)
	interval = int(maxval/100)+1
	i = 0
	prev_x = -999
	prev_y = -999
	for w in ways:
		if (i%interval)==0:
			PrintStatus(i, maxval)
		i+=1
		set_lines = []
		min_x = 99999
		min_y = 99999
		max_x = -99999
		max_y = -99999
		for b in range (len(w)):
			y = scale * ( y_max - merc_y(w[b][0]))
			x = scale * ( merc_x(w[b][1]) - x_min)
			# filter lines too close to each other
			dist = abs(x-prev_x) + abs(y-prev_y)
			if dist > usefilter:
				set_lines.append(( x, y ))
				prev_x = x
				prev_y = y

				if x < min_x:
					min_x = x
				if x > max_x:
					max_x = x
				if y < min_y:
					min_y = y
				if y > max_y:
					max_y = y

		areasize = max( (max_x-min_x), (max_y-min_y) )
		if len(set_lines)>2 and areasize>min_size:
			gfx.polygon(set_lines, fillcolor, None)
			#gfx.polygon(set_lines, None, (255,0,0))
	print ("")


def Draw_Scale (gfx, img, coord_minmax):
	imgw = img.width
	imgh = img.height

	target_dist=100
	margin_pixel=15
	height_pixel=12
	stepcount=10

	lon_mid = coord_minmax.lon_min + (coord_minmax.lon_max - coord_minmax.lon_min)
	lat_mid = coord_minmax.lat_min + (coord_minmax.lat_max - coord_minmax.lat_max)

	lon_use = lon_mid + 1.0
	dist_1degree = haversine.distance((lat_mid, lon_mid), (lat_mid, lon_use))

	target_degree = target_dist/dist_1degree
	lon_use = lon_mid + target_degree

	dist = haversine.distance((lat_mid, lon_mid), (lat_mid, lon_use))
	pixelwidth = abs(scale*(merc_x(lon_mid)-merc_x(lon_use)))

	startx = imgw-(margin_pixel+pixelwidth)
	starty = imgh-(margin_pixel+height_pixel)

	x = startx
	fill=True

	for step in range(stepcount):
		x_next = startx + (float(step+1)*(pixelwidth/float(stepcount)))
		if fill:
			usefill=(255,255,255)
			fill=False
		else:
			usefill=(0,0,0)
			fill=True
		gfx.rectangle((x,starty, x_next, starty+height_pixel),usefill,(0,0,0))
		x = x_next

	font = ImageFont.truetype("arial.ttf", 11)
	gfx.text((startx - (gfx.textsize("0", font)[0]/2), starty - 15), "0", (0,0,0), font)
	gfx.text(((imgw-margin_pixel)-(gfx.textsize(str(target_dist), font)[0]/2), starty - 15), str(target_dist), (0,0,0), font)


def Draw_Stations (gfx, img, st_list):
	imgw = img.width
	imgh = img.height
	
	font = ImageFont.truetype("arial.ttf", 10)

	for st in st_list:
		y = scale * ( y_max - merc_y(st[1]))
		x = scale * ( merc_x(st[2]) - x_min)

		#img.putpixel((int(x),int(y)),(255,0,0))
		gfx.rectangle((int(x),int(y),int(x)+1,int(y)+1),(0,0,0),0)
		gfx.text((x+3,y+3), st[0], (0,0,0), font)


#### START


if len(sys.argv) <= 1:
	exit("Give DB file name as argument")

if not os.path.isfile(sys.argv[1]):
	exit("Cannot open file: " + sys.argv[1])

imgname = sys.argv[1].split('.')[0] + ".png"
imgw = 2000
imgh = 2000
lake_minsize = 8

conn = sqlite3.connect(sys.argv[1])
c = conn.cursor()

im = Image.new('RGB', (imgw, imgh), (255,255,255))
draw = ImageDraw.Draw(im)


## Country borders
border_ways_coordinates = []

prev_setnum = 1
tmpset = []

query_result = c.execute ( "SELECT setnum, latitude, longitude FROM borders;" )

for qr in query_result:
	if qr[0]!=prev_setnum:
		border_ways_coordinates.append ( tmpset )
		prev_setnum = qr[0]
		tmpset = []
	tmpset.append ( (qr[1], qr[2]) )
border_ways_coordinates.append (tmpset)


## Water
water_ways_coordinates = []

prev_setnum = 1
tmpset = []

query_result = c.execute ( "SELECT setnum, latitude, longitude FROM water;" )

for qr in query_result:
	if qr[0]!=prev_setnum:
		water_ways_coordinates.append ( tmpset )
		prev_setnum = qr[0]
		tmpset = []
	tmpset.append ( (qr[1], qr[2]) )
water_ways_coordinates.append (tmpset)



## Railways
railway_ids_diesel = RailwaySet(0)
railway_ids_1500 = RailwaySet(1500)
railway_ids_3000 = RailwaySet(3000)
railway_ids_15k = RailwaySet(15000)
railway_ids_25k = RailwaySet(25000)
railway_ids_othervoltage = RailwaySet(9999)

all_railway_ways = []

query_result = c.execute ( "SELECT way_id, electrified, tracks, voltage FROM railways WHERE abandoned=False AND ( ( usage='main' OR usage='branch' ) OR ( usage='' AND service='' ) );" )


for qr in query_result:
	all_railway_ways.append (qr[0])

	if qr[3]==0:
		railway_ids_diesel.PutItem(qr[0], qr[2])
	elif qr[3]==1500:
		railway_ids_1500.PutItem(qr[0], qr[2])
	elif qr[3]==3000:
		railway_ids_3000.PutItem(qr[0], qr[2])
	elif qr[3]==15000:
		railway_ids_15k.PutItem(qr[0], qr[2])
	elif qr[3]==25000:
		railway_ids_25k.PutItem(qr[0], qr[2])
	elif qr[3]==1500:
		railway_ids_1500.PutItem(qr[0], qr[2])
	else:
		railway_ids_othervoltage.PutItem(qr[0], qr[2])

railway_ids_diesel.Process()
railway_ids_1500.Process()
railway_ids_3000.Process()
railway_ids_15k.Process()
railway_ids_25k.Process()
railway_ids_othervoltage.Process()

## Create node map of all raiways
PT_DIFF = 0.001
ST_DIFF = 0.0025

class RailwayNode:
	def __init__ (self,pos,startcount):
		self.coord=pos
		self.count=startcount

def MakeNodes(nodelist, coordset, weight):
	for cs in coordset:
		node0_found = False
		node1_found = False
		for n in nodelist:
			diff0 = CoordDiff(n.coord, cs[0])
			diff1 = CoordDiff(n.coord, cs[-1])
			if diff0 < PT_DIFF:
				n.count+=weight
				node0_found=True
				break
			if diff1 < PT_DIFF:
				n.count+=weight
				node1_found=True
				break
		if node0_found==False:
			nodelist.append( RailwayNode(cs[0], weight) )
		if node1_found==False:
			nodelist.append( RailwayNode(cs[-1], weight) )


railway_nodes = []

MakeNodes(railway_nodes, railway_ids_diesel.coordinates_single, 1)
MakeNodes(railway_nodes, railway_ids_diesel.coordinates_double, 1)
MakeNodes(railway_nodes, railway_ids_1500.coordinates_single, 1)
MakeNodes(railway_nodes, railway_ids_1500.coordinates_double, 1)
MakeNodes(railway_nodes, railway_ids_3000.coordinates_single, 1)
MakeNodes(railway_nodes, railway_ids_3000.coordinates_double, 1)
MakeNodes(railway_nodes, railway_ids_15k.coordinates_single, 1)
MakeNodes(railway_nodes, railway_ids_15k.coordinates_double, 1)
MakeNodes(railway_nodes, railway_ids_25k.coordinates_single, 1)
MakeNodes(railway_nodes, railway_ids_25k.coordinates_double, 1)
MakeNodes(railway_nodes, railway_ids_othervoltage.coordinates_single, 1)
MakeNodes(railway_nodes, railway_ids_othervoltage.coordinates_double, 1)

count_multi = 0

c.execute( "SELECT name, latitude, longitude FROM stations;" )
stations = c.fetchall()

node_stations = []
node_nodes1 = []
node_nodes2 = []

for n in railway_nodes:
	if n.count==1:
		node_nodes1.append((str(n.count),n.coord[0],n.coord[1]))
	if n.count>2:
		node_nodes2.append((str(n.count),n.coord[0],n.coord[1]))
#		for st in stations:
#			diff = CoordDiff(n.coord, (st[1],st[2]))
#			if diff < ST_DIFF:
#				try:
#					node_stations.index(st)
#				except:
#					node_stations.append(st)
#				break


## Scan for min and max bounds
border_minmax = ScanBounds (border_ways_coordinates, "borders")

#print ( "MIN:", border_minmax.lat_min, border_minmax.lon_min, "  MAX:", border_minmax.lat_max, border_minmax.lon_max )	

y_max = merc_y(border_minmax.lat_max)
y_min = merc_y(border_minmax.lat_min)
x_max = merc_x(border_minmax.lon_max)
x_min = merc_x(border_minmax.lon_min)

delta_x = x_max - x_min
delta_y = y_max - y_min

#print ("MIN:", x_min, y_min, "  MAX:", x_max, y_max, "  DELTA:", delta_x, delta_y)

scalex = imgw / delta_x
scaley = imgh / delta_y

scale = min(scalex, scaley)

Draw_Way_Polygon(water_ways_coordinates, draw, im, (160,190,255), lake_minsize, "water")
#Draw_Way_Coordinates(water_ways_coordinates, draw, im, ((0,0,255), 1), "waters")

Draw_Way_Coordinates(border_ways_coordinates, draw, im, ((0,0,0), 1), "borders")

Draw_Way_Coordinates(railway_ids_diesel.coordinates_single, draw, im, RailwayLineStyle(0,1), "railway diesel.1")
Draw_Way_Coordinates(railway_ids_diesel.coordinates_double, draw, im, RailwayLineStyle(0,2), "railway diesel.2")
Draw_Way_Coordinates(railway_ids_1500.coordinates_single, draw, im,RailwayLineStyle(1500,1), "railway 1500.1")
Draw_Way_Coordinates(railway_ids_1500.coordinates_double, draw, im,RailwayLineStyle(1500,2), "railway 1500.2")
Draw_Way_Coordinates(railway_ids_3000.coordinates_single, draw, im,RailwayLineStyle(3000,1), "railway 3000.1")
Draw_Way_Coordinates(railway_ids_3000.coordinates_double, draw, im,RailwayLineStyle(3000,2), "railway 3000.2")
Draw_Way_Coordinates(railway_ids_15k.coordinates_single, draw, im,RailwayLineStyle(15000,1), "railway 15k.1")
Draw_Way_Coordinates(railway_ids_15k.coordinates_double, draw, im,RailwayLineStyle(15000,2), "railway 15k.2")
Draw_Way_Coordinates(railway_ids_25k.coordinates_single, draw, im,RailwayLineStyle(25000,1), "railway 25k.1")
Draw_Way_Coordinates(railway_ids_25k.coordinates_double, draw, im,RailwayLineStyle(25000,2), "railway 25k.2")
Draw_Way_Coordinates(railway_ids_othervoltage.coordinates_double, draw, im,RailwayLineStyle(9999,1), "railway other.1")
Draw_Way_Coordinates(railway_ids_othervoltage.coordinates_double, draw, im,RailwayLineStyle(9999,2), "railway other.2")

#Draw_Stations(draw, im, node_stations)
#Draw_Stations(draw, im, node_nodes2)
#Draw_Stations(draw, im, node_nodes1)
Draw_Stations(draw, im, stations)

Draw_Scale(draw, im, border_minmax)

im.save(imgname)

conn.close

