
import sys
import time

MAX_ARTICLES_FETCHED = 200

reload(sys)
sys.setdefaultencoding("utf-8")


sys.setdefaultencoding('utf-8')

API_URL = 'http://health.groups.yahoo.com/group/VasectomyPain/message/%d'


import cookielib, urllib2
cj = cookielib.CookieJar()
opener = urllib2.build_opener(urllib2.HTTPCookieProcessor(cj))

msg_id = 1
while True:
    msgfile = 'content/msg_%05d.html' % msg_id
    try:
        with open(msgfile) as f: pass
        print "Skipping: %s" % msgfile
    except IOError as e:
        print "Restarting at %s" % msgfile
        break
    msg_id += 1

files_downloaded = 0
while True:
    full_url = API_URL % msg_id
    msgfile = 'content/msg_%05d.html' % msg_id
    r = opener.open(full_url)
    data = r.read()
    with open(msgfile ,'w') as fout:
        fout.write(data)
        print 'Wrote %s' % msgfile
    msg_id += 1
    files_downloaded += 1
    if files_downloaded == MAX_ARTICLES_FETCHED:
        msg = "Downloaded %d articles: waiting 2 minutes before continuing" % MAX_ARTICLES_FETCHED
        print msg
        files_downloaded = 0
        time.sleep(60*2)
