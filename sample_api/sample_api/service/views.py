from django.shortcuts import render
from django.http import JsonResponse

# Create your views here.

ids = {
    '20151683': True,
    '20151682': False,
}

def fee(request):
    student_id = request.GET.get('student_id', '')
    result = {
        'result': 0,
    }
    if student_id in ids:
        if ids.get(student_id):
            result.update({'is_paid':True})
        else:
            result.update({'is_paid':False})
    else:
        result.update({'result': 1})
    return JsonResponse(result)
