#kubectl delete ns demo
#kubectl create ns demo
#kubectl config set-context --current --namespace=demo

kubectl delete deployments --all --force --grace-period=0
kubectl delete rs --all --force --grace-period=0
kubectl delete pods --all --force --grace-period=0
