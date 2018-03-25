from django.urls import path
from service import views

app_name = 'service'
urlpatterns = [
    path('fee', views.fee, name='fee'),
]
