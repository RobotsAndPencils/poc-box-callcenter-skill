if [ "$1" == "" ]; then
  existingFunction=true
fi

# utility
function abs_path {
  echo $(cd $1;pwd)
}

funcName=TranscriptionTest
zipName=AWSTranscriptionLambda.zip
zipPath=../bin
region="us-east-1"
sourcePath=../bin/Debug/netcoreapp2.0/publish
role="arn:aws:iam::901211063728:role/bs-log-role"

if [ -e $zipPath/$zipName ]; then
  echo "killing old file"
  rm $zipPath/$zipName
else
  echo "no old file"
fi

# get destination absolute path
absZipDest=$(abs_path $zipPath)

# save current location
currentPath=$(pwd)
cd $sourcePath

# package publish folder
zip -r $absZipDest/$zipName *

# restore old location
cd $currentPath

awsCreateFunction () {
echo "#### Creating New Function ######"
  aws lambda create-function \
  --region $region \
  --function-name $funcName \
  --zip-file fileb://$zipPath/$zipName \
  --role $role \
  --handler AWSTranscriptionLamda::AWSTranscriptionLamda.Function::FunctionHandler \
  --runtime dotnetcore2.0 \
  --description "transcription"

}

awsUpdateFunction () {
  echo "#### Updating Function ######"
  aws lambda update-function-code \
  --function-name $funcName \
  --zip-file fileb://$zipPath/$zipName \
  --publish
}

if [ -z "$existingFunction" ]; then
  awsCreateFunction
else
  awsUpdateFunction
fi
