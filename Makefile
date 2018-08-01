build: 
	curl -Lsfo build.sh https://cakebuild.net/download/bootstrapper/osx
	chmod +x build.sh
	./build.sh
	rm build.sh