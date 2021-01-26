#!/bin/bash
mongodb1=`getent hosts mongodb1 | awk '{ print $1 }'`
mongodb2=`getent hosts mongodb2 | awk '{ print $1 }'`
mongodb3=`getent hosts mongodb3 | awk '{ print $1 }'`

port=${PORT:-27017}
echo "Waiting for startup.."
until mongo --eval 'quit(db.runCommand({ ping: 1 }).ok ? 0 : 2)' &>/dev/null; do
    printf '.'
    sleep 1
done
echo "Started.."

echo ~~~~~~~~setup replic time now: `date +"%T" `
echo ${mongodb1},  ${mongodb2},  ${mongodb3}
mongo <<EOF
    var cfg = {
        "_id": "rs",
        "protocolVersion": 1,
        "members": [
            {
                "_id": 0,
                "host": "${mongodb1}:${port}",
                "priority": 2
            },
            {
                "_id": 1,
                "host": "${mongodb2}:${port}",
                "priority": 0
            },
            {
                "_id": 2,
                "host": "${mongodb3}:${port}",
                "priority": 0
            }
        ]
    };
    rs.initiate(cfg, { force: true });
    rs.reconfig(cfg, { force: true });
    rs.slaveOk();
    db.getMongo().setReadPref('nearest');
    db.getMongo().setSlaveOk();
EOF
sleep 30
USER = "root"
PASSWORD = "123456"
echo ${USER}, ${PASSWORD}
echo ~~~~~~~~~~setup user auth time now: `date +"%T" `
mongo <<EOF
    use admin
    db.createUser(
        {
            "user": "${USER}",
            "pwd": "${PASSWORD}",
            "roles": [ "readWrite"]
        }
    )
EOF
