
items = [ 'foo1', 'foo2', 'foo3', 'foo4', 'foo5', 'foo6' ]

i = 0
for item in items:
    print ( str(i) + "  " + item )
    if i==1:
        print ( str(i) + " delete " + item )
        items.remove(item)
    i+=1

print("After:")
for i in range(len(items)):
    print ( str(i) + "  " + items[i] )

